/** @file ChapterPage.xaml.cs
 *  @brief 編輯章節內容

 *  由於章節(chapter)由多個段落(paragraph)所組成，所以這個頁面分成兩部分：左邊是 ListView 以資料綁定
    的方式顯示段落子標題以及第一張照片，右邊以 TextBox 和 ListView 顯示選定的段落之文字與照片集。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2024/1/22 */

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace Imgs2Epub
{
    public partial class ChapterPage : Page
    {
        /// <summary>
        ///  被拖曳的 item index。
        /// </summary>
        private int m_fromParaIndex = -1;
        private int m_fromPhotoIndex = -1;
        private bool m_isDragging = false;

        /// 內部的更新狀態:
        private Boolean m_textBoxesAreChanged = false;
        private Boolean m_checkBoxesIsChanged = false;
        private Boolean m_photoDateIsChanged = false;
        private Boolean m_photoCountIsChanged = false;

        /// 由於 ListView 不支援由非 UI 執行緒增減物件，因此必須將匯入的照片先放在這裡:
        private List<Photo> m_addedPhotos = new List<Photo>();

        /// 產生預覽檔案過程中發生的錯誤。
        private String m_lastError = String.Empty;

        public ChapterPage()
        {   InitializeComponent();  }

        #region 把段落內容載入到畫面的控制項.
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            Paragraph para = null;

            /// 如果本章尚未有任何的段落，產生第一個空白段落:
            if (app.CurChap.Body.Paragraphs.Count == 0)
            {
                para = new Paragraph(app.CurChap);
                para.Date = app.CurAlbum.FirstDate;
                app.CurChap.Body.Paragraphs.Add(para);
                app.CurChap.IsModified = true;
            }

            /// 將 CurChap.Body.Paragraphs 綁定到 ParaListView 並且自動選擇最後一段:
            ParaListView.ItemsSource = app.CurChap.Body.Paragraphs;
            if (app.CurChap.Body.Paragraphs.Count > 0)
            {
                int index = app.CurChap.Body.Paragraphs.Count - 1;
                ParaListView.SelectedIndex = index;
                para = app.CurChap.Body.Paragraphs[index];
                ParaListView.ScrollIntoView(para);
            }

            /// 將 para 的內容顯示在右側:
            ViewParagraphDetails(para);
        }

        /// <summary>
        ///  當選擇切換的時候，將 app.CurPara 的內容顯示在右側的控制項。
        /// </summary>
        private void ViewParagraphDetails(Paragraph para)
        {
            var app = (App)Application.Current;

            /// 先記錄原本的 app.CurPara 修改內容:
            if (app.CurPara != null) {  CheckParagraphChanges();  }

            if (para != null)
            {
                /// 把 para 的內容複製到右側的 TextBox 和 CheckBox 上:
                PhotoDatePicker.SelectedDate = para.Date;
                PhotoDatePicker.IsEnabled = true;
                PhotoDateCheckBox.IsChecked = para.DateIsVisible;
                PhotoDateCheckBox.IsEnabled = true;

                LocationTextBox.Text = para.Location;
                LocationTextBox.IsEnabled = true;
                LocationCheckBox.IsChecked = para.LocationIsVisible;
                LocationCheckBox.IsEnabled = true;

                ParaTitleTextBox.Text = para.Title;
                ParaTitleTextBox.IsEnabled = true;

                ContextTextBox.Text = para.Context;
                ContextTextBox.IsEnabled = true;

                PhotoListView.ItemsSource = para.Photos;
                PhotoListView.IsEnabled = true;
            }
            else
            {
                /// 重置控制項的狀態、然後禁用之:
                PhotoDatePicker.SelectedDate = app.CurAlbum.FirstDate;
                PhotoDatePicker.IsEnabled = false;
                PhotoDateCheckBox.IsChecked = false;
                PhotoDateCheckBox.IsEnabled = false;

                LocationTextBox.Text = String.Empty;
                LocationTextBox.IsEnabled = false;
                LocationCheckBox.IsChecked = false;
                LocationCheckBox.IsEnabled = false;

                ParaTitleTextBox.Text = String.Empty;
                ParaTitleTextBox.IsEnabled = false;
                ContextTextBox.Text = String.Empty;
                ContextTextBox.IsEnabled = false;

                PhotoListView.ItemsSource = null;
                PhotoListView.IsEnabled = false;
            }

            app.CurPara = para;

            /// 清除相關的旗標:
            m_textBoxesAreChanged = false;
            m_photoDateIsChanged = false;
            m_checkBoxesIsChanged = false;
            app.CurPhoto = null;
        }

        /// <summary>
        ///  當選擇改變的時候，在右邊顯示目前的選定的段落內容。
        /// </summary>
        private void ParaListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {   ViewParagraphDetails(ParaListView.SelectedItem as Paragraph);  }

        /// <summary>
        ///  控制項內容被編輯的時候更改旗標。
        /// </summary>
        private void OnTextBoxChanged(object sender, TextChangedEventArgs e)
        {   m_textBoxesAreChanged = true;  }

        private void OnCheckedUnchecked(object sender, RoutedEventArgs e)
        {   m_checkBoxesIsChanged = true;   }

        private void OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {   m_photoDateIsChanged = true;  }
        #endregion

        #region 儲存篇章的現況。
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  儲存現況並回到上一頁。
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();

            /// 因為照片數量改變了，所以也儲存 album.xml:
            var app = Application.Current as App;
            if (app.CurAlbum.IsModified) {  app.CurAlbum.SaveXml();  }

            if (this.NavigationService.CanGoBack)
            {   this.NavigationService.GoBack();  }
        }

        /// <summary>
        ///  檢查各項旗標，決定要不要將控制項內容抄錄回到 app.CurPara 物件。
        /// </summary>
        private void CheckParagraphChanges()
        {
            var app = Application.Current as App;

            if (m_textBoxesAreChanged)
            {
                app.CurPara.Title = ParaTitleTextBox.Text;
                app.CurPara.Location = LocationTextBox.Text;
                app.CurPara.Context = ContextTextBox.Text;
                if (String.IsNullOrEmpty(app.CurPara.Location) == false)
                {   app.CurPara.LocationIsVisible = true;  }

                m_textBoxesAreChanged = false;
                app.CurChap.IsModified = true;
            }

            if (m_photoDateIsChanged == true)
            {
                DateTime? dt = PhotoDatePicker.SelectedDate;
                if (dt.HasValue) {  app.CurPara.Date = dt.Value;  }
                PhotoDateCheckBox.IsChecked = true;
                app.CurPara.DateIsVisible = true;

                m_photoDateIsChanged = false;
                m_checkBoxesIsChanged = true;
                app.CurChap.IsModified = true;

                /// 如果段落的日期比 LastDate 新，則更新之:
                if (DateTime.Compare(app.CurAlbum.LastDate, app.CurPara.Date) < 0)
                {
                    app.CurAlbum.LastDate = app.CurPara.Date;
                    app.CurAlbum.IsModified = true;
                }
            }

            if (m_checkBoxesIsChanged == true)
            {
                if (PhotoDateCheckBox.IsChecked == true)
                {   app.CurPara.DateIsVisible = true;  } else {  app.CurPara.DateIsVisible = false;  }

                if (LocationCheckBox.IsChecked == true)
                {   app.CurPara.LocationIsVisible = true;  } else {  app.CurPara.LocationIsVisible = false;  }

                m_checkBoxesIsChanged = false;
                app.CurChap.IsModified = true;
            }
        }

        /// <summary>
        ///  如果使用者關閉此頁面的時候，是按下右上角的 X 按鈕，準備了這個函式在 NavigationWindow_Closing() 被呼叫。
        /// </summary>
        public void SaveChanges()
        {
            var app = Application.Current as App;
            Debug.WriteLine("ChapterPage.SaveChanges() is invoked.");

            /// 確保在畫面上的控制項所做的變更有抄寫回 paragraph 當中:
            if (app.CurPara != null) {  CheckParagraphChanges();  }

            /// 檢查各 paragraph 當中，有沒有照片的縮圖需要重新產生的:         
            foreach (Paragraph para in app.CurChap.Body.Paragraphs)
            {
                foreach (Photo photo in para.Photos)
                {
                    if (photo.ThumbSize != para.ThumbSize)
                    {
                        String srcPathName = Path.Combine(app.CurChap.Folder, photo.FileName);
                        String[] tokens = new String[] {  app.CurChap.Folder, "thumbs", photo.FileName  };
                        String thumbPathName = Path.Combine(tokens);
                        photo.ThumbSize = para.ThumbSize;

                        if (photo.Orient == Photo.Orientation.Landscape)
                        {
                            ImagingHelper.GetResizedImageFile(srcPathName, thumbPathName,
                                ImagingHelper.DefThumbWidth * photo.ThumbSize,
                                ImagingHelper.DefThumbHeight * photo.ThumbSize, true);
                        }
                        else
                        {
                            ImagingHelper.GetResizedImageFile(srcPathName, thumbPathName,
                                ImagingHelper.HalfThumbWidth * photo.ThumbSize,
                                ImagingHelper.DefThumbHeight * photo.ThumbSize, true);
                        }
                    }
                }
            }

            if (m_photoCountIsChanged)
            {
                app.CurChap.RecountPhotos();
                app.CurChap.OnPropertyChanged("PhotoCount");
                app.CurAlbum.IsModified = true;
                m_photoCountIsChanged = false;
            }

            /// 立即儲存 chapter.xml:
            if (app.CurChap.IsModified)
            {   app.CurChap.SaveXml();  }
        }

        /// <summary>
        ///  在副執行緒中進行網頁的建構。
        /// </summary>
        private Task BuildPreviewPage()
        {
            var app = Application.Current as App;
            m_lastError = String.Empty;

            WebSiteBuilder builder = new WebSiteBuilder(app.CurAlbum);
            if (builder.Build() == false) {  m_lastError = builder.LastError;   }

            return Task.CompletedTask;
        }

        /// <summary>
        ///  檢查 (curChap.Directory).xhtml 的版本並重建，然後前往 PreviewPage。。
        /// </summary>
        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            SaveChanges();

            MessageLabel.Text = Properties.Resources.BuildPreviewMsg;
            ImpExpProgBar.IsIndeterminate = true;
            await Task.Run(() => BuildPreviewPage());
            ImpExpProgBar.IsIndeterminate = false;

            if (String.IsNullOrEmpty(m_lastError) == true)
            {
                MessageLabel.Text = Properties.Resources.Done;
                NavigationService.Navigate(new PreviewPage(app.CurChap.Directory + ".xhtml"));
            }
            else
            {
                MessageBox.Show(m_lastError);
                MessageLabel.Text = m_lastError;
            }
        }
        #endregion

        #region 拖曳段落的順序。
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  在滑鼠左鍵按下的時候，取得被拖曳段落的位置 m_fromParaIndex。
        /// </summary>
        private void Paragraph_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            m_fromParaIndex = ParaListView.ItemContainerGenerator.IndexFromContainer(item);
            m_isDragging = false;
        }

        /// <summary>
        ///  為了讓點選跟拖曳的動作不要有所衝突，當滑鼠有拖動行為的時候，才開始拖曳。
        /// </summary>
        private void Paragraph_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if ((m_fromParaIndex != -1) && (m_isDragging == false))
                {
                    ListViewItem item = ParaListView.ItemContainerGenerator.ContainerFromIndex(m_fromParaIndex) as ListViewItem;
                    DataObject data = new DataObject("Paragraph", m_fromParaIndex);
                    DragDrop.DoDragDrop(item, data, DragDropEffects.Move);
                    m_isDragging = true;
                }
            }
        }

        private void Paragraph_DragOver(object sender, DragEventArgs e)
        {   e.Effects = DragDropEffects.Move;  }

        /// <summary>
        ///  在拖曳結束的時候，取得拖曳目標 item 的位置 toIndex，然後執行 Move 的動作。
        /// </summary>
        private void Paragraph_Drop(object sender, DragEventArgs e)
        {
            App app = Application.Current as App;
            ListViewItem item = sender as ListViewItem;
            int toIndex = ParaListView.ItemContainerGenerator.IndexFromContainer(item);

            /// 拖曳的來源是個段落，執行的動作是將 m_fromParaIndex 的 Paragraph 移動到目前的位置:
            if (e.Data.GetDataPresent("Paragraph"))
            {
                if (m_fromParaIndex != toIndex)
                {
                    app.CurChap.Body.Paragraphs.Move(m_fromParaIndex, toIndex);
                    app.CurChap.IsModified = true;
                }
            }

            /// 拖曳的來源是張照片，執行的動作是把照片從 app.CurPara 移動到 destPara:
            if (e.Data.GetDataPresent("Photo"))
            {
                Paragraph destPara = item.DataContext as Paragraph;
                if (destPara.Equals(app.CurPara) == false)
                {
                    Photo photo = app.CurPara.Photos[m_fromPhotoIndex];
                    app.CurPara.Photos.RemoveAt(m_fromPhotoIndex);
                    app.CurPara.EvaluateTempSize();

                    destPara.Photos.Add(photo);
                    destPara.EvaluateTempSize();
                    app.CurChap.IsModified = true;
                }
            }

            m_fromParaIndex = -1;
            m_fromPhotoIndex = -1;
            m_isDragging = false;
            e.Handled = true;
        }
        #endregion

        #region 拖曳照片的順序。
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  在滑鼠左鍵按下的時候，取得被拖曳段落的位置 m_fromPhotoIndex。
        /// </summary>
        private void Photo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            m_fromPhotoIndex = PhotoListView.ItemContainerGenerator.IndexFromContainer(item);
            m_isDragging = false;
        }

        /// <summary>
        ///  為了讓點選跟拖曳的動作不要有所衝突，當滑鼠有拖動行為的時候，才開始拖曳。
        /// </summary>
        private void Photo_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if ((m_fromPhotoIndex != -1) && (m_isDragging == false))
                {
                    ListViewItem item = PhotoListView.ItemContainerGenerator.ContainerFromIndex(m_fromPhotoIndex) as ListViewItem;
                    DataObject data = new DataObject("Photo", m_fromPhotoIndex);
                    DragDrop.DoDragDrop(item, data, DragDropEffects.Move);
                    m_isDragging = true;
                }
            }
        }

        private void Photo_DragOver(object sender, DragEventArgs e)
        {   e.Effects = DragDropEffects.Move;  }

        /// <summary>
        ///  在拖曳結束的時候，取得拖曳目標 item 的位置 toIndex，然後執行 Move 的動作。
        /// </summary>
        private void Photo_Drop(object sender, DragEventArgs e)
        {
            App app = Application.Current as App;
            ListViewItem item = sender as ListViewItem;
            int toIndex = PhotoListView.ItemContainerGenerator.IndexFromContainer(item);

            /// 拖曳的來源是張照片，執行的動作是將 m_fromPhotoIndex 的 Photo 移動到目前的位置:
            if (e.Data.GetDataPresent("Photo"))
            {
                if (m_fromPhotoIndex != toIndex)
                {   app.CurPara.Photos.Move(m_fromPhotoIndex, toIndex);  }
            }

            if (toIndex == 0)
            {   app.CurPara.OnPropertyChanged("ThumbImageSrc");  }

            m_fromPhotoIndex = -1;
            m_isDragging = false;
            app.CurChap.IsModified = true;
            e.Handled = true;
        }
        #endregion

        #region 新增或刪除段落。
        /// ///////////////////////////////////////////////////////////////////////////////////////     
        /// <summary>
        ///  新增段落，並且設為 app.CurPara。
        /// </summary>
        private void AddParaButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            Paragraph para = new Paragraph(app.CurChap);
            para.Date = app.CurAlbum.FirstDate;

            int index = app.CurChap.Body.Paragraphs.Count;
            app.CurChap.Body.Paragraphs.Add(para);
            ParaListView.SelectedIndex = index;
            ParaListView.ScrollIntoView(para);

            ViewParagraphDetails(para);
            app.CurChap.IsModified = true;
        }

        /// <summary>
        ///  刪除 app.CurPara 以及此段落下所有的圖片檔案。
        /// </summary>
        private void DeleteParaButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            if (app.CurPara == null) {  return;  }
            int index = ParaListView.SelectedIndex;
            if (index == -1) {  index = 0;  }

            MessageBoxResult result = MessageBox.Show
                (Properties.Resources.ConfirmDeleteParagraph, Properties.Resources.AppTitle, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) {  return;  }

            /// 先將 app.CurPara 由 Body.Paragraphs 中移除:
            Paragraph toBeDel = app.CurPara;
            app.CurChap.Body.Paragraphs.Remove(app.CurPara);
            app.CurChap.IsModified = true;

            /// 再讓頁面顯示下一個段落:
            if (app.CurChap.Body.Paragraphs.Count > 0)
            {
                if (index >= app.CurChap.Body.Paragraphs.Count)
                {   index = app.CurChap.Body.Paragraphs.Count - 1;  }

                ParaListView.SelectedIndex = index;
                Paragraph para = app.CurChap.Body.Paragraphs[index];
                ViewParagraphDetails(para);
            }
            else
            {   ViewParagraphDetails(null);  }

            /// 立即儲存 chapter.xml:
            app.CurChap.SaveXml();
            m_photoCountIsChanged = true;

            /// 嘗試刪除 app.CurPara 中所有的照片檔案:
            String tempPath = Path.Combine(app.CurChap.Folder, "thumbs");
            foreach (Photo photo in toBeDel.Photos)
            {
                try
                {
                    String photoPathName = Path.Combine(app.CurChap.Folder, photo.FileName);
                    File.Delete(photoPathName);

                    String fileTitle = Path.GetFileNameWithoutExtension(photo.FileName);
                    String xhtmlPathName = Path.Combine(app.CurChap.Folder, fileTitle + "wj6.xhtml");
                    File.Delete(xhtmlPathName);

                    String thumbPathName = Path.Combine(tempPath, photo.FileName);
                    File.Delete(thumbPathName);
                }
                catch { }
            }
        }
        #endregion

        #region 加入照片到段落。
        /// /////////////////////////////////////////////////////////////////////////////////////// 
        private async void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;

            /// 必須先選擇段落，預設選擇最後一段:
            if (app.CurPara == null)
            {
                Paragraph para;

                if (app.CurChap.Body.Paragraphs.Count == 0)
                {
                    para = new Paragraph(app.CurChap);
                    para.Date = app.CurAlbum.FirstDate;
                    app.CurChap.Body.Paragraphs.Add(para);
                    ParaListView.SelectedIndex = 0;
                }
                else
                {
                    int index = app.CurChap.Body.Paragraphs.Count - 1;
                    ParaListView.SelectedIndex = index;
                    para = app.CurChap.Body.Paragraphs[index];
                }

                /// 將 para 的內容顯示在右側:
                ViewParagraphDetails(para);
            }

            /// 開啟檔案對話框，選取照片檔案:
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = Properties.Resources.ImportPhotoMsg;
            dialog.Filter = "JPEG files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG files (*.png)|*.png";
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = true;
            dialog.Multiselect = true;

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                MessageLabel.Text = Properties.Resources.LoadingPhotos;
                ImpExpProgBar.IsIndeterminate = true;
                await Task.Run(() => ImportPhotoFiles(dialog.FileNames));
                ImpExpProgBar.IsIndeterminate = false;

                if (m_addedPhotos.Count > 0)
                {
                    /// 由於 ListView 不支援由非 UI 執行緒增減物件，因此必須在這裡才執行加入的動作:
                    foreach (Photo photo in m_addedPhotos)
                    {
                        app.CurPara.Photos.Add(photo);
                        app.CurPara.ThumbSize = photo.ThumbSize;
                    }

                    m_addedPhotos.Clear();
                    app.CurChap.IsModified = true;
                    m_photoCountIsChanged = true;
                    app.CurPara.OnPropertyChanged("ThumbImageSrc");
                }

                if (String.IsNullOrEmpty(m_lastError))
                {   MessageLabel.Text = Properties.Resources.Done;  }
                else
                {
                    MessageBox.Show(m_lastError);
                    MessageLabel.Text = m_lastError;
                }
            }
        }

        /// <summary>
        ///  藉由拖放來載入影像檔案。
        /// </summary>
        private async void PhotoListView_Drop(object sender, DragEventArgs e)
        {
            App app = Application.Current as App;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false) {  return;  }
            String[] files = e.Data.GetData(DataFormats.FileDrop, true) as String[];
            if (files.Length == 0) {  return;  }
            e.Handled = true;

            /// 必須先選擇段落，預設選擇最後一段:
            if (app.CurPara == null)
            {
                Paragraph para;

                if (app.CurChap.Body.Paragraphs.Count == 0)
                {
                    para = new Paragraph(app.CurChap);
                    para.Date = app.CurAlbum.FirstDate;
                    app.CurChap.Body.Paragraphs.Add(para);
                    ParaListView.SelectedIndex = 0;
                }
                else
                {
                    int index = app.CurChap.Body.Paragraphs.Count - 1;
                    ParaListView.SelectedIndex = index;
                    para = app.CurChap.Body.Paragraphs[index];
                }

                /// 將 para 的內容顯示在右側:
                ViewParagraphDetails(para);
            }

            /// 啟動執行緒來執行 ImportPhotoFiles() 以匯入檔案:
            MessageLabel.Text = Properties.Resources.LoadingPhotos;
            ImpExpProgBar.IsIndeterminate = true;
            await Task.Run(() => ImportPhotoFiles(files));
            ImpExpProgBar.IsIndeterminate = false;

            if (m_addedPhotos.Count > 0)
            {
                /// 由於 ListView 不支援由非 UI 執行緒增減物件，因此必須在這裡才執行加入的動作:
                foreach (Photo photo in m_addedPhotos)
                {
                    app.CurPara.Photos.Add(photo);
                    app.CurPara.ThumbSize = photo.ThumbSize;
                }

                m_addedPhotos.Clear();
                app.CurChap.IsModified = true;
                m_photoCountIsChanged = true;
                app.CurPara.OnPropertyChanged("ThumbImageSrc");
            }

            if (String.IsNullOrEmpty(m_lastError))
            {   MessageLabel.Text = Properties.Resources.Done;  }
            else
            {
                MessageBox.Show(m_lastError);
                MessageLabel.Text = m_lastError;
            }
        }

        /// <summary>
        ///  載入多個影像檔案到 app.CurPara 當中。
        /// </summary>
        private Task ImportPhotoFiles(String[] files)
        {
            App app = Application.Current as App;
            m_lastError = String.Empty;

            /// 先計算段落裡面目前已經有幾個 blocks:
            int blocks = 0;
            foreach (Photo photo in app.CurPara.Photos)
            {
                if (photo.Orient == Photo.Orientation.Landscape)
                {   blocks += 2;  } else {  ++blocks;  }
            }

            for (int i=0; i<files.Length; ++i)
            {
                String srcPathName = files[i];
                String fileTitle = Path.GetFileNameWithoutExtension(srcPathName);
                String fileExt = Path.GetExtension(srcPathName).ToLower();
                if (fileExt.Equals(".jpg") || fileExt.Equals(".jpeg") || fileExt.Equals(".png"))
                {
                    /// 先嘗試複製檔案，自動產生不重複的檔案名稱:
                    int j = 1;
                    String fileName = fileTitle + fileExt;
                    String destPathName = Path.Combine(app.CurChap.Folder, fileName);
                    while (File.Exists(destPathName) == true)
                    {
                        fileName = fileTitle + j.ToString() + fileExt;
                        destPathName = Path.Combine(app.CurChap.Folder, fileName);
                        ++j;
                    }

                    /// 將原始影像檔案複製到篇章的目錄內:
                    try
                    {   File.Copy(srcPathName, destPathName, true);  }
                    catch (Exception ex)
                    {   m_lastError = ex.Message;  return Task.CompletedTask;  }

                    /// 取得原始圖片的尺寸:
                    Photo newPhoto = new Photo(app.CurChap);
                    newPhoto.FileName = fileName;

                    System.Drawing.Size size = ImagingHelper.GetImageSize(destPathName);
                    if (size.Height > size.Width)
                    {  newPhoto.Orient = Photo.Orientation.Portrait;  blocks += 1;  }
                    else {  blocks +=2; }

                    /// 預計畫面寬度可以裝 8 個寬度單位，如果 blocks 為 2，可以裝兩個 4X 的縮圖:
                    if (blocks < 3) {  newPhoto.ThumbSize = 4;  }
                    /* 如果 blocks 為 3，可以裝兩個 3X 的縮圖:
                    else if (blocks == 3) {  newPhoto.ThumbSize = 3;  } */
                    /// 如果 blocks 為 4，可以裝兩個 2X 的縮圖:
                    else if (blocks < 5) {  newPhoto.ThumbSize = 2;  }
                    /// 如果更多 blocks，縮圖就一律是 1X:
                    else {  newPhoto.ThumbSize = 1;  }

                    /// 圖片檔案複製成功，在 thumbs 資料夾產生縮圖，如果無法產生縮圖則刪除已複製的原圖:
                    String thumbPath = Path.Combine(app.CurChap.Folder, "thumbs");
                    if (Directory.Exists(thumbPath) == false) {  Directory.CreateDirectory(thumbPath);  }

                    String thumbPathName = Path.Combine(thumbPath, fileName);
                    if (newPhoto.Orient == Photo.Orientation.Landscape)
                    {
                        if (ImagingHelper.GetResizedImageFile(destPathName, thumbPathName,
                                ImagingHelper.DefThumbWidth * newPhoto.ThumbSize,
                                ImagingHelper.DefThumbHeight * newPhoto.ThumbSize, true) == false)
                        {
                            try {  File.Delete(destPathName);  } catch { }
                            m_lastError = ImagingHelper.LastError;
                            return Task.CompletedTask;
                        }
                    }
                    else
                    {
                        if (ImagingHelper.GetResizedImageFile(destPathName, thumbPathName,
                            ImagingHelper.HalfThumbWidth * newPhoto.ThumbSize,
                            ImagingHelper.DefThumbHeight * newPhoto.ThumbSize, true) == false)
                        {
                            try {  File.Delete(destPathName);  } catch { }
                            m_lastError = ImagingHelper.LastError;
                            return Task.CompletedTask;
                        }
                    }

                    /// 圖片複製成功、縮圖產製成功，故將新的照片加入到 app.CurPara 當中:
                    m_addedPhotos.Add(newPhoto);
                }
            }

            return Task.CompletedTask;
        }
        #endregion

        #region 編輯或刪除照片。
        /// /////////////////////////////////////////////////////////////////////////////////////// 
        private void PhotoListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App app = Application.Current as App;
            if (PhotoListView.SelectedItem != null)
            {   app.CurPhoto = PhotoListView.SelectedItem as Photo;  }
            else {  app.CurPhoto = null;  }
        }

        private void Photo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            App app = Application.Current as App;
            ListViewItem item = sender as ListViewItem;
            app.CurPhoto = item.DataContext as Photo;
            e.Handled = true;

            EditPhotoButton_Click(null, null);
        }

        /// <summary>
        ///  開啟 PhotoDialog 來編輯 app.CurPhoto。 
        /// </summary>
        private void EditPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurPhoto == null) {  return;  }

            PhotoDialog dialog = new PhotoDialog();
            if (dialog.ShowDialog() == true)
            {
                if (String.IsNullOrEmpty(dialog.LastError) == false)
                {   MessageBox.Show(dialog.LastError);  }

                app.CurPhoto.OnPropertyChanged("ThumbImageSrc");
                app.CurPhoto.OnPropertyChanged("Description");
            }
        }

        /// <summary>
        ///  刪除目前選定的照片。
        /// </summary>
        private void DeletePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurPhoto == null) {  return;  }

            MessageBoxResult result = MessageBox.Show
                (Properties.Resources.ConfirmDeletePhoto, Properties.Resources.AppTitle, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) {  return;  }

            /// 從 app.CurPara 中移除 app.CurPhoto:
            int index = PhotoListView.SelectedIndex;
            app.CurPara.Photos.Remove(app.CurPhoto);
            if (index == 0) {  app.CurPara.OnPropertyChanged("FirstThumbSrc");  }
            app.CurChap.IsModified = true;
            m_photoCountIsChanged = true;

            Photo toBeDel = app.CurPhoto;
            app.CurPhoto = null;

            /// 重新計算此段落的 thumb size:
            app.CurPara.EvaluateTempSize();

            /// 嘗試刪除圖片檔案及其縮圖:
            try
            {
                String photoPathName = Path.Combine(app.CurChap.Folder, toBeDel.FileName);
                File.Delete(photoPathName);

                String fileTitle = Path.GetFileNameWithoutExtension(toBeDel.FileName);
                String xhtmlPathName = Path.Combine(app.CurChap.Folder, fileTitle + "wj6.xhtml");
                File.Delete(xhtmlPathName);

                String thumbPath = Path.Combine(app.CurChap.Folder, "thumbs");
                String thumbPathName = Path.Combine(thumbPath, toBeDel.FileName);
                File.Delete(thumbPathName);
            }
            catch { }
        }
        #endregion
    }
}
