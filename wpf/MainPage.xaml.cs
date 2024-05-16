/** @file MainPage.xaml.cs
 *  @brief 編輯相簿基本資料

 *  在這個頁面編輯 app.CurAlbum 以及 app.CurAlbum.Body 的內容，但是在 ChapterPage 才會編輯 Chapter 的
    內容。在此不使用資料綁定，而是在 Page_Loaded() 中把 app.CurAlbum 及其 Body 的內容顯示於頁面上那些
    TextBox 控制項，而對於 Body 中的 Chapters，在這個頁面用 ListView 去陳列、更名、拖曳重序它們，但不
    編輯其內容。要返回或關閉的時候呼叫 SaveChanges() 檢查哪些 TextBox 經過更動，更新 app.CurAlbum 並
    且儲存 XML 檔案。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2024/3/12 */

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Imgs2Epub
{
    public partial class MainPage : Page
    {
        /// <summary>
        ///  頁面上的輸入欄位是否變動。
        /// </summary>
        private Boolean m_albumTextAreChanged = false;
        private Boolean m_albumDatesAreChanged = false;

        /// <summary>
        ///  被拖曳的 item index。
        /// </summary>
        private int m_fromIndex = -1;
        private Boolean m_isDragging = false;

        /// 產生預覽檔案過程中發生的錯誤。
        private String m_lastError = String.Empty;

        public MainPage()
        {   InitializeComponent();  }

        #region 載入與儲存頁面上的控制項內容。
        /// <summary>
        ///  載入 CurAlbum 的內容到頁面上。
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;       
            if (app.CurAlbum == null) {  return;  }
            app.MainWindow.Title = app.CurAlbum.Title + " - " + Properties.Resources.AppTitle;
            Debug.WriteLine("MainPage.Page_Loaded(): " + app.CurAlbum.Directory);

            CoverImage.Source = app.CurAlbum.CoverImageSrc;
            ChapListView.ItemsSource = app.CurAlbum.Body.Chapters;

            AlbumTitleTextBox.Text = app.CurAlbum.Title;
            AuthorTextBox.Text = app.CurAlbum.Author;
            LocationTextBox.Text = app.CurAlbum.Location;

            FirstDatePicker.SelectedDate = app.CurAlbum.FirstDate;
            LastDatePicker.SelectedDate = app.CurAlbum.LastDate;
            m_albumDatesAreChanged = false;

            PrefaceTextBox.Text = app.CurAlbum.Body.Text;
            m_albumTextAreChanged = false;
            app.CurAlbum.IsModified = false;

            /// 預設選擇最後的篇章:
            if (app.CurChap != null)
            {   ChapListView.SelectedItem = app.CurChap;  }
            else if (app.CurAlbum.Body.Chapters.Count > 0)
            {
                int lastIndex = app.CurAlbum.Body.Chapters.Count - 1;
                ChapListView.SelectedIndex = lastIndex;
                app.CurChap = app.CurAlbum.Body.Chapters[lastIndex];
            }
        }

        private void AlbumTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            App app = Application.Current as App;
            m_albumTextAreChanged = true;
            app.CurAlbum.IsModified = true;
            AlbumInfo.IsCollectionModified = true;
        }

        private void AlbumDateChanged(object sender, SelectionChangedEventArgs e)
        {
            App app = Application.Current as App;
            m_albumDatesAreChanged = true;
            app.CurAlbum.IsModified = true;
        }

        private void PrefaceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            App app = Application.Current as App;
            m_albumTextAreChanged = true;
            app.CurAlbum.IsModified = true;
        }

        /// <summary>
        ///  當使用者直接按下視窗的 [X] 關閉的時候，讓 NavigationWindow_Closing() 呼叫這個函式以儲存現況。
        /// </summary>
        public void SaveChanges()
        {
            var app = (App)Application.Current;

            if (m_albumTextAreChanged == true)
            {
                app.CurAlbum.Title = AlbumTitleTextBox.Text;
                app.CurAlbum.Author = AuthorTextBox.Text;
                app.CurAlbum.Location = LocationTextBox.Text;
                app.CurAlbum.Body.Text = PrefaceTextBox.Text;
                app.CurAlbum.OnPropertyChanged("Title");
            }

            if (m_albumDatesAreChanged == true)
            {
                DateTime? dt = FirstDatePicker.SelectedDate;
                if (dt.HasValue) {  app.CurAlbum.FirstDate = dt.Value;  }

                dt = LastDatePicker.SelectedDate;
                if (dt.HasValue) { app.CurAlbum.LastDate = dt.Value;  }

                app.CurAlbum.OnPropertyChanged("FirstDate");
            }

            if (app.CurAlbum.IsModified == true)
            {   app.CurAlbum.SaveXml();  }

            if (AlbumInfo.IsCollectionModified)
            {   app.SaveAlbumsXml();  }
        }

        /// <summary>
        ///  儲存現況並回到上一頁。
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            if (this.NavigationService.CanGoBack)
            {   this.NavigationService.GoBack();  }
        }
        #endregion

        #region 編輯或上傳封面照片.
        /// ---------------------------------------------------------------------------------------
        /// <summary>
        ///  進入 CoverPage 以編輯封面圖片。
        /// </summary>
        private void EditCoverButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            NavigationService.Navigate(new CoverPage());
        }

        private void CoverImage_MouseClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                SaveChanges();
                NavigationService.Navigate(new CoverPage());
            }
        }

        /// <summary>
        ///  以 OpenFileDialog 選擇一個圖檔作為相簿封面圖片。
        /// </summary>
        private void UploadCoverButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = Properties.Resources.ImportCoverMsg;
            dialog.Filter = "JPEG Image files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Image files (*.png)|*.png";
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;

            /// 開啟檔案對話框，選取圖片檔案:
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {  UploadCoverImage(dialog.FileName);  }
        }

        /// <summary>
        ///  以拖放的方式上傳封面照片。
        /// </summary>
        private void CoverImage_Drop(object sender, DragEventArgs e)
        {
            App app = Application.Current as App;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false) {  return;  }
            String[] files = e.Data.GetData(DataFormats.FileDrop, true) as String[];
            if (files.Length > 0) {  UploadCoverImage(files[0]);  }
            e.Handled = true;
        }

        /// <summary>
        ///  將 pathName 縮放另存為 cover.jpg。
        /// </summary>
        private void UploadCoverImage(String pathName)
        {
            String fileExt = Path.GetExtension(pathName).ToLower();
            if (fileExt.Equals(".jpg") || fileExt.Equals(".jpeg") || fileExt.Equals(".png"))
            {
                /// 將選定的檔案縮放另存為 cover.jpg:
                App app = Application.Current as App;
                String coverPathName = Path.Combine(app.CurAlbum.ContentFolder, "cover.jpg");
                if (ImagingHelper.GetResizedImageFile(pathName, coverPathName, 768, 1024, true) == false)
                {
                    MessageBox.Show(ImagingHelper.LastError);
                    MessageLabel.Text = ImagingHelper.LastError;
                    return;
                }

                /// 更改 app.CurAlbum 中的紀錄:
                app.CurAlbum.CoverFile = "cover.jpg";
                app.CurAlbum.IsModified = true;
                AlbumInfo.IsCollectionModified = true;

                /// 重新載入封面圖片:
                CoverImage.Source = app.CurAlbum.CoverImageSrc;

                /// 建立新的縮圖，縮圖建立失敗不會顯示訊息方塊:
                String thumbPath = Path.Combine(app.DataDir, "Thumbs");
                if (Directory.Exists(thumbPath) == false) { Directory.CreateDirectory(thumbPath); }

                String destPathName = Path.Combine(thumbPath, app.CurAlbum.Directory + ".jpg");
                if (ImagingHelper.GetResizedImageFile(coverPathName, destPathName, 300, 400, false) == false)
                {   MessageLabel.Text = ImagingHelper.LastError;  return; }

                /// 完成:
                app.CurAlbum.OnPropertyChanged("ThumbImageSrc");
                MessageLabel.Text = Properties.Resources.Done;
            }
        }

        /// <summary>
        ///  刪除目前的封面圖片檔案。
        /// </summary>
        private void RemoveCoverButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            String fileName = app.CurAlbum.CoverFile;
            if (String.IsNullOrEmpty(fileName) == true) {  return;  }

            MessageBoxResult result = MessageBox.Show
                (Properties.Resources.ConfirmDeleteCover, Properties.Resources.AppTitle, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) {  return;  }

            /// 移除 AlbumInfo 當中的設定:
            app.CurAlbum.CoverFile = String.Empty;
            app.CurAlbum.Body.Cover.CoverRawFile = String.Empty;            
            app.CurAlbum.IsModified = true;

            /// 重設目前顯示的封面圖:
            String assetPathName = Path.Combine(app.AssetDir, "cover_none.jpg");
            CoverImage.Source = ImagingHelper.LoadImageFile(assetPathName);

            /// 刪除相簿封面檔案及其縮圖:
            String coverPathName = Path.Combine(app.CurAlbum.ContentFolder, fileName);
            if (File.Exists(coverPathName) == true) {  File.Delete(coverPathName);  }

            String thumbPath = Path.Combine(app.DataDir, "Thumbs");
            if (Directory.Exists(thumbPath) == false) {  Directory.CreateDirectory(thumbPath);  }

            String thumbPathName = Path.Combine(thumbPath, app.CurAlbum.Directory + ".jpg");
            if (File.Exists(thumbPathName) == true) {  File.Delete(thumbPathName);  }

            app.CurAlbum.OnPropertyChanged("ThumbImageSrc");
        }
        #endregion

        #region 新建或編輯篇章.
        /// ---------------------------------------------------------------------------------------
        /// <summary>
        ///  產生一個新的空白篇章。
        /// </summary>
        private void AddChapButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            ChapterInfo chapter = app.CurAlbum.Body.CreateChapter();
            if (chapter == null)
            {
                MessageBox.Show(app.CurAlbum.LastError);
                Debug.WriteLine(app.CurAlbum.LastError);
                return;
            }

            /// 為了避免因當機而導致的資料丟失，先儲存 album.xml。
            SaveChanges();

            /// 將新建的篇章設為目前選定的篇章:
            app.CurChap = chapter;
            ChapListView.SelectedItem = chapter;
        }

        /// <summary>
        ///  當選擇的項目改變的時候，同步改變 app.CurChap。
        /// </summary>
        private void ChapListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App app = Application.Current as App;
            if (ChapListView.SelectedItem != null)
            {   app.CurChap = ChapListView.SelectedItem as ChapterInfo;  }
            else {  app.CurChap = null;  }
        }

        /// <summary>
        ///  進入 ChapterPage 以編輯篇章內容。
        /// </summary>
        private void EditChapButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurChap == null) {  return; }

            /// 為了避免因當機而導致的資料丟失，先儲存 album.xml。
            SaveChanges();

            /// 如果選定的章節內容還沒載入記憶體的話，就載入之:
            if (app.CurChap.Body == null)
            {
                if (app.CurChap.LoadXml() == false)
                {
                    MessageBox.Show(app.CurChap.LastError);
                    Debug.WriteLine(app.CurChap.LastError);
                    return;
                }
            }

            /// 前往 ChapterPage 進行編輯:
            NavigationService.Navigate(new ChapterPage());
        }

        /// <summary>
        ///  雙擊篇章即可進入 ChapterPage 編輯之。
        /// </summary>
        private void Chapter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            App app = Application.Current as App;
            app.CurChap = item.DataContext as ChapterInfo;
            e.Handled = true;

            /// 為了避免因當機而導致的資料丟失，先儲存 album.xml。
            SaveChanges();

            /// 如果選定的章節內容還沒載入記憶體的話，就載入之:
            if (app.CurChap.Body == null)
            {
                if (app.CurChap.LoadXml() == false)
                {
                    MessageBox.Show(app.CurChap.LastError);
                    Debug.WriteLine(app.CurChap.LastError);
                    return;
                }
            }

            /// 前往 ChapterPage 進行編輯:
            NavigationService.Navigate(new ChapterPage());
        }
        #endregion

        #region 拖放篇章順序。
        /// <summary>
        ///  在滑鼠左鍵按下的時候，取得被拖曳 item 的位置 m_fromIndex。
        /// </summary>
        private void Chapter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            m_fromIndex = ChapListView.ItemContainerGenerator.IndexFromContainer(item);
            m_isDragging = false;
        }

        /// <summary>
        ///  為了讓點選跟拖曳的動作不要有所衝突，當滑鼠有拖動行為的時候，才開始拖曳。
        /// </summary>
        private void Chapter_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if ((m_fromIndex != -1) && (m_isDragging == false))
                {
                    ListViewItem item = ChapListView.ItemContainerGenerator.ContainerFromIndex(m_fromIndex) as ListViewItem;
                    DragDrop.DoDragDrop(item, item.DataContext, DragDropEffects.Move);
                    m_isDragging = true;
                }
            }
        }

        private void Chapter_DragOver(object sender, DragEventArgs e)
        {   e.Effects = DragDropEffects.Move;  }

        /// <summary>
        ///  在拖曳結束的時候，取得拖曳目標 item 的位置 toIndex，然後執行 Move 的動作。
        /// </summary>
        private void Chapter_Drop(object sender, DragEventArgs e)
        {
            App app = Application.Current as App;
            ListViewItem item = sender as ListViewItem;
            int toIndex = ChapListView.ItemContainerGenerator.IndexFromContainer(item);
            if (m_fromIndex != toIndex) {  app.CurAlbum.Body.Chapters.Move(m_fromIndex, toIndex);  }

            m_fromIndex = -1;
            m_isDragging = false;
            app.CurAlbum.IsModified = true;
        }
        #endregion

        #region 更名或刪除篇章.
        /// <summary>
        ///  開啟 RenameChapDlg 輸入新的章節標題或目錄名稱。
        /// </summary>
        private void RenameChapButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurChap == null) {  return;  }

            /// 開啟對話框來詢問使用者:
            RenameChapDlg dialog = new RenameChapDlg();
            dialog.SubDirectory = app.CurChap.Directory;
            dialog.ChapTitle = app.CurChap.Title;
            if (dialog.ShowDialog() == false) {  return; }

            /// 子目錄名稱是否更動？
            if (dialog.SubDirectory.Equals(app.CurChap.Directory) == false)
            {
                if (app.CurChap.Rename(dialog.SubDirectory) == false)
                {   MessageBox.Show(app.CurChap.LastError);  return;  }
            }

            if (dialog.ChapTitle.Equals(app.CurChap.Title) == false)
            {
                app.CurChap.Title = dialog.ChapTitle;
                app.CurAlbum.IsModified = true;
            }
        }

        /// <summary>
        ///  刪除目前的篇章。
        /// </summary>
        private void DeleteChapButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurChap == null) {  return; }

            MessageBoxResult result = MessageBox.Show
                (Properties.Resources.ConfirmDeleteChapter, Properties.Resources.AppTitle, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) {  return;  }

            /// 先從 CurAlbum.Body.Chapters 中移除選定的章節:
            ChapterInfo toBeDel = app.CurChap;
            int index = ChapListView.SelectedIndex;
            app.CurChap = null;

            app.CurAlbum.Body.Chapters.Remove(toBeDel);
            app.CurAlbum.IsModified = true;
            Debug.WriteLine("MasterPage.DeleteChapButton_Click(): " + toBeDel.Directory);

            /// 嘗試刪除已生成的 .xhtml 檔案。
            try
            {
                String pathName = Path.Combine(app.CurAlbum.ContentFolder, toBeDel.Directory + ".xhtml");
                if (File.Exists(pathName)) {  File.Delete(pathName);  }

                /// 遞迴地刪除 toBeDel.Folder 的相簿目錄:
                Directory.Delete(toBeDel.Folder, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
                MessageLabel.Text = ex.Message;
                return;
            }

            /// 預防因當機而造成的資料丟失，先儲存所有資料:
            SaveChanges();

            /// 刪除後自動地選擇下一個篇章:
            if (index == ChapListView.Items.Count) {  --index;  }
            if (index != -1)
            {
                ChapListView.SelectedIndex = index;
                app.CurChap = app.CurAlbum.Body.Chapters[index];
            }
        }
        #endregion

        #region 建構並預覽相簿 HTML 內容。
        /// ---------------------------------------------------------------------------------------
        /// <summary>
        ///  進入 PreviewPage 預覽現在的相簿 HTML 內容。
        /// </summary>
        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();

            MessageLabel.Text = Properties.Resources.BuildPreviewMsg;
            PreviewProgBar.IsIndeterminate = true;
            await Task.Run(() => BuildPreviewPages());
            PreviewProgBar.IsIndeterminate = false;

            if (String.IsNullOrEmpty(m_lastError) == true)
            {
                MessageLabel.Text = Properties.Resources.Done;
                NavigationService.Navigate(new PreviewPage("title.xhtml"));
            }
            else
            {
                MessageBox.Show(m_lastError);
                MessageLabel.Text = m_lastError;
            }
        }

        /// <summary>
        ///  在副執行緒中進行網頁的建構。
        /// </summary>
        private Task BuildPreviewPages()
        {
            App app = Application.Current as App;
            m_lastError = String.Empty;


            WebSiteBuilder builder = new WebSiteBuilder(app.CurAlbum);
            if (builder.Build() == false) {  m_lastError = builder.LastError;   }

            return Task.CompletedTask;
        }
        #endregion
    }
}
