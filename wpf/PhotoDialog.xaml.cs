/// -----------------------------------------------------------------------------------------------
/// <summary>
///  照片編輯器
/// </summary>
/// <remarks>
///  這個視窗包含了一個簡單的相片顯示器，並且實作了另存與旋轉圖片的功能。
/// </remarks>
/// <history>
///  2023/10/11 by Shu-Kai Yang (skyang@csie.nctu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Imgs2Epub
{
    public partial class PhotoDialog : Window
    {
        /// 目前顯示中的圖片:
        private BitmapImage m_bitmap = null;
        private TransformedBitmap m_transformedBmp = null;
        private int m_rotateDrgrees = 0;
        private bool m_isReplaced = false;
        public String LastError = String.Empty;

        /// 來自設定檔的視窗尺寸與位置紀錄:
        static public System.Drawing.Size ViewerSize = new System.Drawing.Size(1280, 600);
        static public System.Drawing.Point ViewerPos = new System.Drawing.Point(0, 0);

        public PhotoDialog()
        {         
            InitializeComponent();

            /// 載入先前儲存的視窗尺寸與位置。
            this.Left = ViewerPos.X;
            this.Top = ViewerPos.Y;
            this.Width = ViewerSize.Width;
            this.Height = ViewerSize.Height;
        }

        /// <summary>
        ///  載入圖片與文字到頁面上的控制項。
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (app.CurPhoto == null) {  return;  }

            String pathName = Path.Combine(app.CurChap.Folder, app.CurPhoto.FileName);
            m_bitmap = ImagingHelper.LoadImageFile(pathName);
            if (m_bitmap == null) {  return;  }

            PhotoViewer.Source = m_bitmap;
            LabelTextBox.Text = app.CurPhoto.Description;
            LabelTextBox.Focus();

            Title = app.CurPhoto.FileName + " (" + (int)m_bitmap.Width + "x" + (int)m_bitmap.Height + ")";
        }

        /// <summary>
        ///  選擇另一個照片檔案來取代目前的照片。
        /// </summary>
        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;

            /// 開啟檔案對話框，選取照片檔案:
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = Properties.Resources.ImportPhotoMsg;
            dialog.Filter = "JPEG files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG files (*.png)|*.png";
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                String fileTitle = Path.GetFileNameWithoutExtension(dialog.FileName);
                String fileExt = Path.GetExtension(dialog.FileName).ToLower();
                String oldFileName = app.CurPhoto.FileName;

                /// 先嘗試複製新檔案，自動產生不重複的檔案名稱:
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
                try {  File.Copy(dialog.FileName, destPathName, true); }
                catch (Exception ex) {  MessageBox.Show(ex.Message);  return;   }

                /// 測試圖片的方向，並且建立縮圖:
                Photo.Orientation orient = Photo.Orientation.Landscape;
                System.Drawing.Size size = ImagingHelper.GetImageSize(destPathName);
                if (size.Height > size.Width) {  orient = Photo.Orientation.Portrait;  }

                /// 圖片檔案複製成功，在 thumbs 資料夾產生縮圖，如果無法產生縮圖則刪除已複製的原圖:
                String thumbPath = Path.Combine(app.CurChap.Folder, "thumbs");
                if (Directory.Exists(thumbPath) == false) { Directory.CreateDirectory(thumbPath); }

                String thumbPathName = Path.Combine(thumbPath, fileName);
                if (orient == Photo.Orientation.Landscape)
                {
                    if (ImagingHelper.GetResizedImageFile(destPathName, thumbPathName,
                            ImagingHelper.DefThumbWidth * app.CurPhoto.ThumbSize,
                            ImagingHelper.DefThumbHeight * app.CurPhoto.ThumbSize, true) == false)
                    {
                        try {  File.Delete(destPathName); } catch { }
                        MessageBox.Show(ImagingHelper.LastError);
                        return;
                    }
                }
                else
                {
                    if (ImagingHelper.GetResizedImageFile(destPathName, thumbPathName,
                        ImagingHelper.HalfThumbWidth * app.CurPhoto.ThumbSize,
                        ImagingHelper.DefThumbHeight * app.CurPhoto.ThumbSize, true) == false)
                    {
                        try { File.Delete(destPathName); } catch { }
                        MessageBox.Show(ImagingHelper.LastError);
                        return;
                    }
                }

                /// 圖片複製成功，縮圖也產生成功，正式以新圖片取代舊圖:
                app.CurPhoto.FileName = fileName;
                app.CurPhoto.Orient = orient;
                m_isReplaced = true;

                /// 重新評估段落的縮圖尺寸:
                app.CurPara.EvaluateTempSize();
                app.CurChap.IsModified = true;

                /// 嘗試刪除舊圖片檔案及其縮圖:
                try
                {
                    String photoPathName = Path.Combine(app.CurChap.Folder, oldFileName);
                    File.Delete(photoPathName);

                    fileTitle = Path.GetFileNameWithoutExtension(oldFileName);
                    String xhtmlPathName = Path.Combine(app.CurChap.Folder, fileTitle + "wj6.xhtml");
                    File.Delete(xhtmlPathName);

                    thumbPathName = Path.Combine(thumbPath, oldFileName);
                    File.Delete(thumbPathName);
                }
                catch { }

                /// 重新載入圖片:
                m_bitmap = ImagingHelper.LoadImageFile(destPathName);
                if (m_bitmap != null)
                {
                    m_rotateDrgrees = 0;
                    PhotoViewer.Source = m_bitmap;
                    Title = app.CurPhoto.FileName + " (" + (int)m_bitmap.Width + "x" + (int)m_bitmap.Height + ")";
                }
            }
        }

        /// <summary>
        ///  將 curPhoto 設為 curAlbum 的 CoverRawFile。
        /// </summary>
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            app.CurAlbum.Body.Cover.CoverRawFile = app.CurChap.Directory + "/" + app.CurPhoto.FileName;
            app.CurAlbum.IsModified = true;

            MessageBox.Show(Properties.Resources.SetCoverPrompt);
        }

        /// <summary>
        ///  將目前的圖片以順時針旋轉 90 度，這裡只使用 RotateTransform 尚未真正地旋轉圖檔。
        /// </summary>
        private void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (m_bitmap == null) {  return;  }

            m_rotateDrgrees += 90;
            if (m_rotateDrgrees == 360) {  m_rotateDrgrees = 0;  }

            RotateTransform transform = null;
            if (m_rotateDrgrees != 0) {  transform = new RotateTransform(m_rotateDrgrees);  }

            if (transform != null)
            {
                m_transformedBmp = new TransformedBitmap();
                m_transformedBmp.BeginInit();
                m_transformedBmp.Source = m_bitmap;
                m_transformedBmp.Transform = transform;
                m_transformedBmp.EndInit();

                PhotoViewer.Source = m_transformedBmp;
                Title = app.CurPhoto.FileName + " (" + (int)m_transformedBmp.Width + "x" + (int)m_transformedBmp.Height + ")";
            }
            else
            {
                PhotoViewer.Source = m_bitmap;
                Title = app.CurPhoto.FileName + " (" + (int)m_bitmap.Width + "x" + (int)m_bitmap.Height + ")";
            }
        }

        /// <summary>
        ///  將圖片檔案複製到外部空間。
        /// </summary>
        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            String fileExt = Path.GetExtension(app.CurPhoto.FileName).ToLower();

            /// 開啟檔案對話方塊以選擇輸出的檔案名稱與類型:
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            if (fileExt.Equals(".png")) {  dialog.Filter = "PNG files (*.png)|*.png";  }
            else {  dialog.Filter = "JPEG files (*.jpg;*.jpeg)|*.jpg;*.jpeg";  }
            dialog.Title = Properties.Resources.ExportPhotoMsg;
            dialog.FileName = app.CurPhoto.FileName;
            dialog.DefaultExt = fileExt;
            dialog.OverwritePrompt = true;

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                String srcPathName = Path.Combine(app.CurChap.Folder, app.CurPhoto.FileName);
                try {  File.Copy(srcPathName, dialog.FileName);  }
                catch (Exception ex) {  MessageBox.Show(ex.Message);  return;  }

                MessageBox.Show(Properties.Resources.FileExportedOK + dialog.FileName);
            }
        }

        /// <summary>
        ///  將影像和文字的狀態寫回 app.CurPhoto。
        /// </summary>
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            Boolean result = m_isReplaced;

            if ((m_rotateDrgrees != 0) && (m_transformedBmp != null))
            {
                /// 以旋轉後的 m_transformedBmp 建立 Bitmap 物件:
                Bitmap bmp = new Bitmap(m_transformedBmp.PixelWidth, m_transformedBmp.PixelHeight,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                BitmapData data = bmp.LockBits(
                    new Rectangle(System.Drawing.Point.Empty, bmp.Size),
                    ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                m_transformedBmp.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                bmp.UnlockBits(data);

                /// 以防萬一，先備份原圖檔:
                String fileTitle = Path.GetFileNameWithoutExtension(app.CurPhoto.FileName);
                String fileExt = Path.GetExtension(app.CurPhoto.FileName).ToLower();
                String bakPathName = Path.Combine(app.CurChap.Folder, fileTitle + "_bak" + fileExt);

                /// 以原檔名儲存旋轉過後的影像 bmp:
                String pathName = Path.Combine(app.CurChap.Folder, app.CurPhoto.FileName);
                try {  File.Move(pathName, bakPathName);  }
                catch (Exception ex) {  LastError = ex.Message;  }

                try {  bmp.Save(pathName);  }
                catch (Exception ex)
                {
                    if (File.Exists(pathName)) { File.Delete(pathName); }
                    File.Move(bakPathName, pathName);
                    LastError = ex.Message;
                }

                /// 刪除備份檔案:
                if (File.Exists(bakPathName))
                { try { File.Delete(bakPathName); } catch { } }

                /// 圖片已經過了旋轉，重新評估段落的縮圖尺寸:
                app.CurPhoto.Orient = Photo.Orientation.Landscape;
                if (bmp.Height > bmp.Width) { app.CurPhoto.Orient = Photo.Orientation.Portrait; }
                app.CurPara.EvaluateTempSize();
                app.CurChap.IsModified = true;

                /// 建立旋轉後的縮圖:
                app.CurPhoto.ThumbSize = app.CurPara.ThumbSize;
                String thumbPath = Path.Combine(app.CurChap.Folder, "thumbs");
                if (Directory.Exists(thumbPath) == false) { Directory.CreateDirectory(thumbPath); }

                String thumbPathName = Path.Combine(thumbPath, app.CurPhoto.FileName);
                if (app.CurPhoto.Orient == Photo.Orientation.Landscape)
                {
                    if (ImagingHelper.GetResizedImageFile(pathName, thumbPathName,
                            ImagingHelper.DefThumbWidth * app.CurPhoto.ThumbSize,
                            ImagingHelper.DefThumbHeight * app.CurPhoto.ThumbSize, true) == false)
                    {   LastError = ImagingHelper.LastError;  }
                }
                else
                {
                    if (ImagingHelper.GetResizedImageFile(pathName, thumbPathName,
                        ImagingHelper.HalfThumbWidth * app.CurPhoto.ThumbSize,
                        ImagingHelper.DefThumbHeight * app.CurPhoto.ThumbSize, true) == false)
                    {   LastError = ImagingHelper.LastError;  }
                }

                result = true;
            }

            if (app.CurPhoto.Description.Equals(LabelTextBox.Text) == false)
            {
                app.CurPhoto.Description = LabelTextBox.Text;
                app.CurChap.IsModified = true;
                result = true;

                /// 變更了的說明文字，將會儲存在 chapter.xml 當中、並且因此更新該檔案的日版本，但是該照片的 wj6.xhtml
                /// 並不知道也要更新，所以故意將它刪除，以便在下一次 BuildChapter() 的時候重新產生。
                String fileTitle = Path.GetFileNameWithoutExtension(app.CurPhoto.FileName);
                String xhtmlPathName = Path.Combine(app.CurChap.Folder, fileTitle + "wj6.xhtml");
                try {  File.Delete(xhtmlPathName);  } catch { }
            }

            /// 當視窗要關閉的時候取回自己的視窗位置與尺寸:
            if (this.WindowState == WindowState.Normal)
            {
                ViewerPos = new System.Drawing.Point((int)this.Left, (int)this.Top);
                ViewerSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
            }
            else
            {
                ViewerPos = new System.Drawing.Point((int)RestoreBounds.Left, (int)RestoreBounds.Top);
                ViewerSize = new System.Drawing.Size((int)RestoreBounds.Size.Width, (int)RestoreBounds.Size.Height);
            }

            DialogResult = result;
        }
    }
}
