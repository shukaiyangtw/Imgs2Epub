/** @file CoverPage.xaml.cs
 *  @brief 生成相簿封面

 *  在 app.CurAlbum.Body 當中有個 CoverSettings 物件，包含了用於生成相簿封面圖檔的設定，在這個頁面將
    它呈現在 Canvas 當中，並且提供改選字體、顏色、背景圖樣等功能，然後把 Canvas 繪製成 cover.jpg。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2024/3/22 */

using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Imgs2Epub
{
    public partial class CoverPage : Page
    {
        /// <summary>
        ///  放置於 Assets 目錄下的背景圖檔集。
        /// </summary>
        private readonly String[] m_assetFiles =
        {
            "cover_default.png",   "cover_paper.png",  "cover_green.png", "cover_purple.png",
            "cover_brick.png",     "cover_teal.png",   "cover_lime.png",  "cover_violet.png",
            "cover_brown.jpg",     "cover_blue.jpg",   "cover_red.jpg",   "cover_white.jpg",
            "cover_dandelion.jpg", "cover_sakura.jpg", "cover_water.jpg", "cover_fiber.png",
            "cover_grass.png",     "cover_dots.png",   "cover_cherrytree.png", "cover_ocean.jpg"
        };

        /// <summary>
        ///  如果 BgAssetView 中的 Image.Source 以檔名路徑字串的形式去綁定 m_assetFiles，會導致這些
        ///  檔案被 lock 而不能再被其他元件讀取，因此預先將它們載入為 BitmapImage 物件再綁定。
        /// </summary>
        public CoverPage()
        {
            InitializeComponent();

            var app = Application.Current as App;
            List<BitmapImage> assets = new List<BitmapImage>();

            for (int i=0; i< m_assetFiles.Length; ++i)
            {
                String pathName = Path.Combine(app.AssetDir, m_assetFiles[i]);
                BitmapImage bmp = ImagingHelper.LoadImageFile(pathName);
                if (bmp != null) {  assets.Add(bmp);  }
             /* else {  MessageLabel.Text = ImagingHelper.LastError;  } */
            }

            BgAssetView.ItemsSource = assets;
        }

        /// <summary>
        ///  載入目前的設定。
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;

            #region 套用 app.CurAlbum.Body.Cover 的編輯設定。
            /// 載入目前的編輯設定，並且設定文字大小與顏色:
            System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb
                (app.CurAlbum.Body.Cover.TextColor.A, app.CurAlbum.Body.Cover.TextColor.R,
                 app.CurAlbum.Body.Cover.TextColor.G, app.CurAlbum.Body.Cover.TextColor.B);

            SolidColorBrush brush = new SolidColorBrush(color);
            TitleTextBox.Foreground = brush;
            AuthorBlock.Foreground = brush;
            DateBlock.Foreground = brush;
            LocationBlock.Foreground = brush;

            System.Windows.Media.FontFamily fontFamily = new System.Windows.Media.FontFamily(app.CurAlbum.Body.Cover.FontFamily);
            TitleTextBox.FontFamily = fontFamily;
            AuthorBlock.FontFamily = fontFamily;
            DateBlock.FontFamily = fontFamily;
            LocationBlock.FontFamily = fontFamily;

            TitleTextBox.FontSize = app.CurAlbum.Body.Cover.TextSize;
            int smallerSize = (int)Math.Floor((TitleTextBox.FontSize * 2) / 3);
            AuthorBlock.FontSize = smallerSize;
            DateBlock.FontSize = smallerSize;
            LocationBlock.FontSize = smallerSize;

            System.Drawing.FontStyle style = (System.Drawing.FontStyle)app.CurAlbum.Body.Cover.FontStyle;
            if (style.HasFlag(System.Drawing.FontStyle.Bold))
            {
                TitleTextBox.FontWeight = FontWeights.Bold;
                AuthorBlock.FontWeight = FontWeights.Bold;
                DateBlock.FontWeight = FontWeights.Bold;
                LocationBlock.FontWeight = FontWeights.Bold;
            }

            if (style.HasFlag(System.Drawing.FontStyle.Italic))
            {
                TitleTextBox.FontStyle = FontStyles.Italic;
                AuthorBlock.FontStyle = FontStyles.Italic;
                DateBlock.FontStyle = FontStyles.Italic;
                LocationBlock.FontStyle = FontStyles.Italic;
            }

            /// 載入背景影像:
            if (app.CurAlbum.Body.Cover.BgIndex < BgAssetView.Items.Count)
            {
                BgAssetView.SelectedIndex = app.CurAlbum.Body.Cover.BgIndex;
                BgAssetView_SelectionChanged(null, null);
            }

            /// 將 app.CurAlbum 的內容顯示到畫面上:
            TitleTextBox.Text = app.CurAlbum.Title;
            AuthorBlock.Text = app.CurAlbum.Author;
            DateBlock.Text = app.CurAlbum.DateStr;
            LocationBlock.Text = app.CurAlbum.Location;

            /// 顯示封面照片:
            if (String.IsNullOrEmpty(app.CurAlbum.Body.Cover.CoverRawFile) == false)
            {
                String pathName = Path.Combine(app.CurAlbum.ContentFolder, app.CurAlbum.Body.Cover.CoverRawFile);
                BitmapImage bmp = ImagingHelper.LoadImageFile(pathName);
                if (bmp != null)
                {
                    CoverPhotoImage.Source = bmp;
                    CoverPhotoImage.Visibility = Visibility.Visible;
                }
             /* else {  MessageLabel.Text = ImagingHelper.LastError;  } */
            }
            #endregion
        }

        /// <summary>
        ///  由於 BgAssetView 中的項目已經是 BitmapImage，所以直接取用 ListViewItem 即可。
        /// </summary>
        private void BgAssetView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var app = Application.Current as App;
            if (BgAssetView.SelectedIndex == -1) {  return; }
            if (BgAssetView.SelectedIndex != app.CurAlbum.Body.Cover.BgIndex)
            {
                app.CurAlbum.Body.Cover.BgIndex = BgAssetView.SelectedIndex;
                app.CurAlbum.IsModified = true;
            }

            /// 載入背景影像，並且設定給 CoverBgImage:
            ListViewItem item = BgAssetView.ItemContainerGenerator.ContainerFromIndex(app.CurAlbum.Body.Cover.BgIndex) as ListViewItem;
            BitmapImage bmp = item.DataContext as BitmapImage;
            CoverBgImage.Source = bmp;
        }

        /// <summary>
        ///  儲存現況並回到上一頁。
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {   this.NavigationService.GoBack();  }
        }

        #region 使用 FontDialog 與 ColorDialog 選擇字體與顏色。
        /// <summary>
        ///  開啟 FontDialog 來選擇字型與大小。
        /// </summary>
        private void TextFontButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FontDialog dialog = new System.Windows.Forms.FontDialog();
            dialog.FontMustExist = true;
            dialog.ShowColor = false;
            dialog.ShowEffects = false;

            /// 以目前的設定為預設值:
            var app = Application.Current as App;
            System.Drawing.FontStyle style = (System.Drawing.FontStyle)app.CurAlbum.Body.Cover.FontStyle;
            Font font = new Font(app.CurAlbum.Body.Cover.FontFamily, app.CurAlbum.Body.Cover.TextSize, style);
            dialog.Font = font;

            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;
            try
            {   result = dialog.ShowDialog();  }
            catch (Exception ex)
            {   MessageBox.Show(ex.Message);  return;  }

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                app.CurAlbum.Body.Cover.FontFamily = dialog.Font.FontFamily.Name;
                app.CurAlbum.Body.Cover.TextSize = (int)dialog.Font.Size;
                app.CurAlbum.Body.Cover.FontStyle = (int)dialog.Font.Style;
                app.CurAlbum.IsModified = true;

                /// 套用選定的字體:
                System.Windows.Media.FontFamily fontFamily = new System.Windows.Media.FontFamily(dialog.Font.FontFamily.Name);
                TitleTextBox.FontFamily = fontFamily;
                AuthorBlock.FontFamily = fontFamily;
                DateBlock.FontFamily = fontFamily;
                LocationBlock.FontFamily = fontFamily;

                TitleTextBox.FontSize = dialog.Font.Size;
                int smallerSize = (int)Math.Floor((dialog.Font.Size * 2) / 3);
                AuthorBlock.FontSize = smallerSize;
                DateBlock.FontSize = smallerSize;
                LocationBlock.FontSize = smallerSize;

                if (dialog.Font.Style.HasFlag(System.Drawing.FontStyle.Bold))
                {
                    TitleTextBox.FontWeight = FontWeights.Bold;
                    AuthorBlock.FontWeight = FontWeights.Bold;
                    DateBlock.FontWeight = FontWeights.Bold;
                    LocationBlock.FontWeight = FontWeights.Bold;
                }
                else
                {
                    TitleTextBox.FontWeight = FontWeights.Regular;
                    AuthorBlock.FontWeight = FontWeights.Regular;
                    DateBlock.FontWeight = FontWeights.Regular;
                    LocationBlock.FontWeight = FontWeights.Regular;
                }

                if (dialog.Font.Style.HasFlag(System.Drawing.FontStyle.Italic))
                {
                    TitleTextBox.FontStyle = FontStyles.Italic;
                    AuthorBlock.FontStyle = FontStyles.Italic;
                    DateBlock.FontStyle = FontStyles.Italic;
                    LocationBlock.FontStyle = FontStyles.Italic;
                }
                else
                {
                    TitleTextBox.FontStyle = FontStyles.Normal;
                    AuthorBlock.FontStyle = FontStyles.Normal;
                    DateBlock.FontStyle = FontStyles.Normal;
                    LocationBlock.FontStyle = FontStyles.Normal;
                }
            }
        }

        /// <summary>
        ///  開啟 ColorDialog 來選擇字體顏色
        /// </summary>
        private void TextColorButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            System.Windows.Forms.ColorDialog dialog = new System.Windows.Forms.ColorDialog();
            dialog.Color = app.CurAlbum.Body.Cover.TextColor;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                app.CurAlbum.Body.Cover.TextColor = dialog.Color;
                app.CurAlbum.IsModified = true;

                /// 設定 WPF 的控制項顏色必須使用 Media.Color 類別:
                System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb
                    (dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);

                SolidColorBrush brush = new SolidColorBrush(color);
                TitleTextBox.Foreground = brush;
                AuthorBlock.Foreground = brush;
                DateBlock.Foreground = brush;
                LocationBlock.Foreground = brush;
            }
        }
        #endregion

        /// <summary>
        ///  移除目前的相簿封面照片。
        /// </summary>
        private void RemovePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            app.CurAlbum.Body.Cover.CoverRawFile = String.Empty;
            app.CurAlbum.IsModified = true;

            CoverPhotoImage.Source = null;
            CoverPhotoImage.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        ///  利用 RenderTargetBitmap 的功能，直接把 CoverCanvas 繪製成 bitmap 並存檔。
        /// </summary>
        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            /// 將 CoverCanvas 繪製成 bitmap 之前，要先把它捲動到頂:
            CanvasScroller.ScrollToVerticalOffset(0);
            CanvasScroller.UpdateLayout();

            /// 基於 CoverCanvas 建立 RenderTargetBitmap 圖片物件:
            RenderTargetBitmap bitmap = new RenderTargetBitmap(768, 1024, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(CoverCanvas);

            /// 將 bitmap 儲存為 cover.jpg 檔案:
            var app = Application.Current as App;
            String coverPathName = Path.Combine(app.CurAlbum.ContentFolder, "cover.jpg");
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);
            using (Stream stream = File.Create(coverPathName)) {  encoder.Save(stream);  }

            /// 儲存 album.xml:
            app.CurAlbum.CoverFile = "cover.jpg";
            if (app.CurAlbum.IsModified == true) {  app.CurAlbum.SaveXml();  }
            AlbumInfo.IsCollectionModified = true;

            /// 產生 Thumbs 目錄下的相簿封面縮圖:
            String thumbPath = Path.Combine(app.DataDir, "Thumbs");
            if (Directory.Exists(thumbPath) == false) {  Directory.CreateDirectory(thumbPath);  }

            String destPathName = Path.Combine(thumbPath, app.CurAlbum.Directory + ".jpg");
            if (ImagingHelper.GetResizedImageFile(coverPathName, destPathName, 300, 400, false) == false)
            {   MessageLabel.Text = ImagingHelper.LastError;  return; }

            app.CurAlbum.OnPropertyChanged("ThumbImageSrc");

            /// 返回 MainPage:
            if (this.NavigationService.CanGoBack)
            {   this.NavigationService.GoBack();  }
        }
    }
}
