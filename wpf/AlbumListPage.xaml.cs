/** @file App.xaml.cs
 *  @brief 全域屬性

 *  在本軟體當中每本相簿都是私有儲存空間下的一個目錄，目錄結構類似 ePub 標準，這個頁面將 app.Albums
    所提供的每本相簿以 ListView 呈現，並提供拖放排序的功能。除此之外，相簿的建立、刪除、匯出與匯入
    也在此進行，如果使用者要編輯其中一本相簿，則由這裡前往 MainPage 頁面。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2024/1/22 */

using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Imgs2Epub
{
    public partial class AlbumListPage : Page
    {
        /// <summary>
        ///  跨執行緒傳輸匯入中的新相簿和過程中的錯誤訊息，因為 UIElement 和 ObservableCollection.Add 不支援多執行緒。
        /// </summary>
        private AlbumInfo m_newAlbum = null;
        private String m_lastError = String.Empty;

        public AlbumListPage()
        {   InitializeComponent();  }

        /// <summary>
        ///  在 ShelfView 顯示 AlbumInfo清單。
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            ShelfView.ItemsSource = app.Albums;
            Debug.WriteLine("MainPage.Page_Loaded(): album x " + app.Albums.Count);
        }

        /// <summary>
        ///  前往官方網站。
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {   Process.Start(Properties.Resources.HelpUrl);  }

        #region 前往 MainPage 編輯選定的相簿.
        /// ---------------------------------------------------------------------------------------
        /// <summary>
        ///  在 ShelfView 選擇改變的時候，設定 CurAlbum 並且在 AlbumInfoView 顯示其內容。
        /// </summary>
        private void ShelfView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App app = Application.Current as App;

            if (ShelfView.SelectedItem != null)
            {
                app.CurAlbum = ShelfView.SelectedItem as AlbumInfo;
                AlbumInfoView.DataContext = app.CurAlbum;
            }
            else
            {
                app.CurAlbum = null;
                AlbumInfoView.DataContext = null;
            }
        }

        /// <summary>
        ///  當 ShelfView 被點擊的時候，設定 CurAlbum 並且進入 MainPage 編輯相簿內容。
        /// </summary>
        private void ShelfViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            App app = Application.Current as App;
            app.CurAlbum = item.DataContext as AlbumInfo;
            app.CurChap = null;
            e.Handled = true;

            /// 到了要編輯的時候才載入相簿內容:
            if (app.CurAlbum.Body == null)
            {
                if (app.CurAlbum.LoadXml() == false)
                {
                    Debug.WriteLine(app.CurAlbum.LastError);
                    MessageBox.Show(app.CurAlbum.LastError);
                    return;
                }
            }

            /// 前往 MainPage 進行編輯:
            NavigationService.Navigate(new MainPage());
        }

        /// <summary>
        ///  進入 MainPage 編輯 ShelfView 所選定的相簿項目。
        /// </summary>
        private void EditAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;

            if (app.CurAlbum != null)
            {
                Debug.WriteLine("AlbumListPage.EditButton_Click(): " + app.CurAlbum.Directory);
                app.CurChap = null;

                /// 到了要編輯的時候才載入相簿內容:
                if (app.CurAlbum.Body == null)
                {
                    if (app.CurAlbum.LoadXml() == false)
                    {
                        Debug.WriteLine(app.CurAlbum.LastError);
                        MessageBox.Show(app.CurAlbum.LastError);
                        return;
                    }
                }

                /// 前往 MainPage 進行編輯:
                NavigationService.Navigate(new MainPage());
            }
        }
        #endregion

        #region 新建或刪除相簿專案.
        /// ---------------------------------------------------------------------------------------
        /// <summary>
        ///  建立新的相簿，包括初始化一個新目錄並儲存 album.xml。
        /// </summary>
        private void AddAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            /// 根據目前的日期產生一個不重複的目錄名稱，並建立新相簿的根目錄:
            String dir = DiskHelper.GenerateNameByDate();
            Debug.WriteLine("AlbumListPage.AddAlbumButton_Click(): new album = " + dir);

            App app = Application.Current as App;
            String fullPath = Path.Combine(app.DataDir, dir);
            DirectoryInfo folderInfo = null;

            try
            {   folderInfo = Directory.CreateDirectory(fullPath);  }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
                MessageLabel.Text = ex.Message;
                return;
            }

            /// 建立新的 AlbumInfo 物件:
            AlbumInfo info = new AlbumInfo(dir);
            Debug.WriteLine("new album folder is created:" + fullPath);

            /// 從 Assets 中目錄複製固定的檔案 mimetype, container.xml, style.css 以及 viewer.css 到相簿目錄:
            try
            {
                String srcPathName = Path.Combine(app.AssetDir, "mimetype");
                String destPathName = Path.Combine(fullPath, "mimetype");
                File.Copy(srcPathName, destPathName, true);
                Debug.WriteLine("mimetype is copied.");

                String metaInfPath = Path.Combine(fullPath, "META-INF");
                Directory.CreateDirectory(metaInfPath);
                srcPathName = Path.Combine(app.AssetDir, "container.xml");
                destPathName = Path.Combine(metaInfPath, "container.xml");
                File.Copy(srcPathName, destPathName, true);
                Debug.WriteLine("META-INF\\container.xml is copied.");

                String epubPath = Path.Combine(fullPath, "EPUB");
                Directory.CreateDirectory(epubPath);
                srcPathName = Path.Combine(app.AssetDir, "style.css");
                destPathName = Path.Combine(epubPath, "style.css");
                File.Copy(srcPathName, destPathName, true);
                Debug.WriteLine("EPUB\\style.css is copied.");

                srcPathName = Path.Combine(app.AssetDir, "viewer.css");
                destPathName = Path.Combine(epubPath, "viewer.css");
                File.Copy(srcPathName, destPathName, true);
                Debug.WriteLine("EPUB\\viewer.css is copied.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
                MessageLabel.Text = ex.Message;
                return;
            }

            /// 初始化相簿 UUID 與相簿標題:
            info.Identifier = Guid.NewGuid().ToString();
            info.Title = Properties.Resources.NewAlbumTitle;

            /// 產生新的空白章節，並且儲存 chapter.xml 檔案:
            info.Body = new Album(info);
            if (info.Body.CreateChapter() == null)
            {
                Debug.WriteLine(info.LastError);
                MessageBox.Show(info.LastError);
                MessageLabel.Text = info.LastError;
                return;
            }

            /// 儲存這個新相簿的 album.xml 檔案:
            if (info.SaveXml() == false)
            {
                Debug.WriteLine(info.LastError);
                MessageBox.Show(info.LastError);
                MessageLabel.Text = info.LastError;
                return;
            }

            /// 將新建立的物件加入 Albums 集合以及 ListView 上:
            app.Albums.Insert(0, info);
            app.CurAlbum = info;
            app.CurChap = null;

            AlbumInfoView.DataContext = app.CurAlbum;
            AlbumInfo.IsCollectionModified = true;

            /// 儲存 albums.xml:
            app.SaveAlbumsXml();
            MessageLabel.Text = Properties.Resources.Done;
        }

        /// <summary>
        ///  遞迴地刪除 CurAlbum 所指定的相簿目錄。
        /// </summary>
        private void DeleteAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurAlbum == null) {  return;  }

            MessageBoxResult result = MessageBox.Show
                (Properties.Resources.ConfirmDeleteAlbum, Properties.Resources.AppTitle, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) {  return;  }

            AlbumInfo toBeDel = app.CurAlbum;
            Debug.WriteLine("AlbumListPage.DeleteAlbumButton_Click(): " + toBeDel.Directory);
            MessageLabel.Text = Properties.Resources.DeletingAlbumMsg + ": " + toBeDel.Title + "...";

            /// 先從 Albums 當中移除 CurAlbum:
            app.Albums.Remove(toBeDel);
            app.CurAlbum = null;
            app.CurChap = null;

            /// 立即儲存 albums.xml 以免資料丟失:
            AlbumInfo.IsCollectionModified = true;
            app.SaveAlbumsXml();

            /// 關閉 AlbumInfoView 的顯示:
            AlbumInfoView.DataContext = null;

            try
            {
                /// 如果有快取此相簿的封面縮圖檔案，刪除之:                   
                String[] tokens = new String[] { app.DataDir, "Thumbs", toBeDel.Directory + ".jpg" };
                String pathName = Path.Combine(tokens);
                if (File.Exists(pathName)) {  File.Delete(pathName);  }

                /// 遞迴地刪除 app.CurAlbum 的相簿目錄:
                String fullPath = Path.Combine(app.DataDir, toBeDel.Directory);
                Directory.Delete(fullPath, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
                MessageLabel.Text = ex.Message;
                return;
            }

            MessageLabel.Text = Properties.Resources.Done;
        }
        #endregion

        #region 匯入 .epub 檔案.
        /// ---------------------------------------------------------------------------------------
        /// 非同步地匯入檔案。
        private Task ImportAlbumFile(String pathName)
        {
            ZipArchive archive = null;
            Debug.WriteLine("AlbumListPage.ImportAlbumFile(" + pathName + ")...");
            m_newAlbum = null;

            /// 嘗試以 ZipArchive 開啟 file，如果不是有效的 zip 檔案則返回:
            try
            {
                FileStream stream = File.OpenRead(pathName);
                archive = new ZipArchive(stream, ZipArchiveMode.Read);
            }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                Debug.WriteLine(m_lastError);
                return Task.CompletedTask;
            }

            /// 在解壓縮之前，先檢查 EPUB/album.xml 是否存在:
            Boolean isAlbum = false;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.Equals("EPUB/album.xml"))
                {
                    Debug.WriteLine("EPUB/album.xml is found.");
                    isAlbum = true;
                    break;
                }
            }

            if (isAlbum == false)
            {
                m_lastError = Properties.Resources.NotAnAlbumFile;
                Debug.WriteLine(m_lastError);
                return Task.CompletedTask;
            }

            /// 確定是個 album epub，解壓縮到新建立的目錄 directory:
            App app = Application.Current as App;
            String directory = DiskHelper.GenerateNameByDate();
            String extractPath = Path.Combine(app.DataDir, directory);
            Debug.WriteLine("Extracting to " + extractPath + "...");

            try
            {   archive.ExtractToDirectory(extractPath);  }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                Debug.WriteLine(m_lastError);
                return Task.CompletedTask;
            }

            /// 讀取 EPUB/album.xml:
            AlbumInfo info = new AlbumInfo(directory);
            info.FileName = Path.GetFileName(pathName);

            if (info.LoadXml() == false)
            {
                m_lastError = info.LastError;
                Debug.WriteLine(m_lastError);
                return Task.CompletedTask;
            }

            /// 建立此相簿的縮圖於 Thumbs 目錄下:
            if (String.IsNullOrEmpty(info.CoverFile) == false)
            {
                String thumbPath = Path.Combine(app.DataDir, "Thumbs");
                if (Directory.Exists(thumbPath) == false) {  Directory.CreateDirectory(thumbPath);  } 

                String destPathName = Path.Combine(thumbPath, directory + ".jpg");
                String srcPathName = Path.Combine(info.ContentFolder, info.CoverFile);
                if (ImagingHelper.GetResizedImageFile(srcPathName, destPathName, 300, 400, false) == false)
                {
                    m_lastError = ImagingHelper.LastError;
                    Debug.WriteLine(m_lastError);
                    return Task.CompletedTask;
                }
            }

            /// 嘗試更新 style.css 與 viewer.css
            String curPathName = Path.Combine(info.ContentFolder, "style.css");
            String assetPathName = Path.Combine(app.AssetDir, "style.css");
            if (DiskHelper.IsFileNewerThan(assetPathName, curPathName) == true)
            {
                File.Copy(assetPathName, curPathName, true);    
                Debug.WriteLine("EPUB\\style.css is updated.");
            }

            curPathName = Path.Combine(info.ContentFolder, "viewer.css");
            assetPathName = Path.Combine(app.AssetDir, "viewer.css");
            if (DiskHelper.IsFileNewerThan(assetPathName, curPathName) == true)
            {
                File.Copy(assetPathName, curPathName, true);
                Debug.WriteLine("EPUB\\viewer.css is updated.");
            }

            m_newAlbum = info;
            return Task.CompletedTask;
        }

        /// <summary>
        ///  開啟檔案對話方塊，選擇 .epub 檔案以後呼叫 ImportAlbumFile() 來載入檔案。
        /// </summary>
        private async void ImportAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = Properties.Resources.ImportEpubMsg;
            dialog.Filter = "Album Epub files (*.epub)|*.epub";
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;

            /// 開啟檔案對話框，選取文件檔案:
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                MessageLabel.Text = Properties.Resources.ImportingAlbumMsg + ": " + dialog.FileName + "...";
                ImpExpProgBar.IsIndeterminate = true;
                await Task.Run(() => ImportAlbumFile(dialog.FileName));
                ImpExpProgBar.IsIndeterminate = false;

                /// 將新建立的物件加入 Albums 集合以及 ListView 上:
                if (m_newAlbum != null)
                {
                    App app = Application.Current as App;
                    app.Albums.Insert(0, m_newAlbum);
                    app.CurAlbum = m_newAlbum;
                    AlbumInfoView.DataContext = app.CurAlbum;
                    m_newAlbum = null;

                    /// 儲存 albums.xml:
                    AlbumInfo.IsCollectionModified = true;
                    app.SaveAlbumsXml();
                    MessageLabel.Text = Properties.Resources.Done;
                }
                else
                {
                    MessageBox.Show(m_lastError);
                    MessageLabel.Text = m_lastError;
                }
            }
        }

        /// <summary>
        ///  實現以拖曳的方式選取檔案，呼叫 ImportAlbumFile() 來載入檔案。
        /// </summary>
        private async void ShelfView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                String[] files = e.Data.GetData(DataFormats.FileDrop, true) as String[];
                if (files.Length == 0) {  return;  }
                e.Handled = true;

                String pathName = files[0];
                String fileExt = Path.GetExtension(pathName).ToLower();
                if (fileExt.Equals(".epub") == true)
                {
                    MessageLabel.Text = Properties.Resources.ImportingAlbumMsg + ": " + pathName + "...";
                    ImpExpProgBar.IsIndeterminate = true;
                    await Task.Run(() => ImportAlbumFile(pathName));
                    ImpExpProgBar.IsIndeterminate = false;

                    /// 將新建立的物件加入 Albums 集合以及 ListView 上:
                    if (m_newAlbum != null)
                    {
                        App app = Application.Current as App;
                        app.Albums.Insert(0, m_newAlbum);
                        app.CurAlbum = m_newAlbum;
                        AlbumInfoView.DataContext = app.CurAlbum;
                        m_newAlbum = null;

                        /// 儲存 albums.xml:
                        AlbumInfo.IsCollectionModified = true;
                        app.SaveAlbumsXml();
                        MessageLabel.Text = Properties.Resources.Done;
                    }
                    else
                    {
                        MessageBox.Show(m_lastError);
                        MessageLabel.Text = m_lastError;
                    }
                }
            }
        }
        #endregion

        #region 匯出相簿專案為 .album.epub 或其他檔案.
        /// ---------------------------------------------------------------------------------------
        /// 非同步地匯出檔案。
        private async void ExportAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurAlbum == null) {  return;  }

            /// 指定預設的檔名:
            if (String.IsNullOrEmpty(app.CurAlbum.FileName) == true)
            {
                String dateStr = app.CurAlbum.FirstDate.ToString("yyyy-MM-dd");
                String titleStr = BaseXhtmlBuilder.SafeFileName(app.CurAlbum.Title);
                app.CurAlbum.FileName = dateStr + "_" + titleStr + ".album.epub";
                AlbumInfo.IsCollectionModified = true;
            }

            /// 開啟檔案對話方塊以選擇輸出的檔案名稱與類型:
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Filter = "Epub album files (*.epub)|*.epub|Zipped HTML files (*.zip)|*.zip";
            dialog.Title = Properties.Resources.ExportEpubMsg;
            dialog.FileName = app.CurAlbum.FileName;
            dialog.DefaultExt = ".epub";
            dialog.OverwritePrompt = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MessageLabel.Text = Properties.Resources.ExportingAlbumMsg + ": " + dialog.FileName + "...";
                ImpExpProgBar.IsIndeterminate = true;
                await Task.Run(() => ExportAlbumFile(dialog.FileName));
                ImpExpProgBar.IsIndeterminate = false;

                if (String.IsNullOrEmpty(m_lastError) == true)
                {   MessageLabel.Text = Properties.Resources.FileExportedOK + dialog.FileName;   }
                else
                {
                    MessageBox.Show(m_lastError);
                    MessageLabel.Text = m_lastError;
                }
            }
        }

        /// 非同步地匯出檔案。
        private Task ExportAlbumFile(String pathName)
        {
            App app = Application.Current as App;
            String fileExt = Path.GetExtension(pathName).ToLower();
            m_lastError = String.Empty;
 
            /// 視需要從 album.xml 和 chapter.xml 載入相簿章節，以取得 Chapters 集合:
            if (app.CurAlbum.Body == null)
            {
                if (app.CurAlbum.LoadXml() == false)
                {
                    m_lastError = app.CurAlbum.LastError;
                    Debug.WriteLine(m_lastError);
                    return Task.CompletedTask;
                }
            }

            foreach (ChapterInfo chapter in app.CurAlbum.Body.Chapters)
            {
                if (chapter.LoadXml() == false)
                {
                    m_lastError = chapter.LastError;
                    Debug.WriteLine(m_lastError);
                    return Task.CompletedTask;
                }
            }

            /// 檢查所有的 .xhtml 檔案版本，並且重建較舊的檔案:
            WebSiteBuilder builder = new WebSiteBuilder(app.CurAlbum);
            if (builder.Build() == false)
            {
                m_lastError = builder.LastError;
                Debug.WriteLine(m_lastError);
                return Task.CompletedTask;
            }

            if (fileExt.Equals(".zip") == true)
            {
                /// 如果只是單純地要建立網頁的壓縮檔案，利用 WebSiteBuilder.reateZip() 建立 zip 檔案:
                if (builder.CreateZip(pathName) == false)
                {
                    m_lastError = builder.LastError;
                    Debug.WriteLine(m_lastError);
                    return Task.CompletedTask;
                }
            }
            else if (fileExt.Equals(".epub") == true)
            {
                /// 建立 content.opf 和 toc.ncx，並且和所有的 xml 與 .xhtml 檔案一起壓縮成 .epub 檔案:
                AlbumEpubBuilder albumBuilder = new AlbumEpubBuilder(app.CurAlbum);
                if (albumBuilder.CreateEpub(pathName) == false)
                {
                    m_lastError = albumBuilder.LastError;
                    Debug.WriteLine(m_lastError);
                    return Task.CompletedTask;
                }
            }
            else
            {
                m_lastError = Properties.Resources.UnsupportedFileFormat;
                Debug.WriteLine(m_lastError);
            }

            return Task.CompletedTask;
        }
        #endregion
    }
}
