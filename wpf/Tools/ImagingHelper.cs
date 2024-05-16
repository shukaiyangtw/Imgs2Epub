/** @file ImagingHelper.cs
 *  @brief 常用的影像轉換工具.

 *  這裡實作一些常用的影像檔案轉換工具，主要都是依靠 BitmapDecoder 和 BitmapEncoder 的功能。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/8/24 */

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace Imgs2Epub
{
    public sealed class ImagingHelper
    {
        /// <summary>
        ///  預設的縮圖尺寸。
        /// </summary>
        readonly static public int DefThumbWidth = 240;
        readonly static public int HalfThumbWidth = 120;
        readonly static public int DefThumbHeight = 180;

        /// <summary>
        ///  記錄最後的錯誤訊息。
        /// </summary>
        public static String LastError = String.Empty;

        #region Get image size and bitmap.
        ///<summary>
        ///  利用 BitmapDecoder 快速地取得影像的尺寸。
        /// </summary>
        public static Size GetImageSize(String pathName)
        {
            using (FileStream stream = File.OpenRead(pathName))
            {
                BitmapDecoder decoder = null;
                LastError = String.Empty;

                try {  decoder =  BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);  }
                catch (Exception ex) {  LastError = ex.Message; return new Size(0, 0);  }

                if (decoder.Frames.Count > 0)
                {
                    BitmapFrame frame = decoder.Frames[0];
                    return new Size(frame.PixelWidth, frame.PixelHeight);
                }

                return new Size(0, 0);
            }
        }

        /// <summary>
        ///  要在 Image 當中顯示影像檔案可以簡單地
        ///  Uri uri = new Uri(pathName);
        ///  image.Source = new BitmapImage(uri);
        ///  但如此一來會造成 pathName 所指的檔案被鎖住，而無法更名或刪除，所以先將檔案內容複製到 memory stream
        ///  再建立 BitmapImage 物件並傳回之。
        /// </summary>
        public static BitmapImage LoadImageFile(String pathName)
        {
            Stream fs = null;
            LastError = String.Empty;

            try {  fs = File.Open(pathName, FileMode.Open, FileAccess.Read);  }
            catch (Exception ex) {  LastError = ex.Message; return null;  }

            MemoryStream ms = new MemoryStream();
            fs.CopyTo(ms);
            fs.Close();
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage img = new BitmapImage();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.BeginInit();
            img.StreamSource = ms;
            img.EndInit();
            return img;
        }
        #endregion

        /// <summary>
        ///  這個函式把 srcPathName 縮放為 destWidth x destHeight 的影像，並且儲存成 destPathName 檔案。
        /// </summary>
        public static Boolean GetResizedImageFile(String srcPathName, String destPathName, int destWidth, int destHeight, Boolean isClipped = false)
        {
            Debug.Write("ImagingHelper.GetResizedImageFile(" + srcPathName + " to " + destPathName + ", "
                + destWidth + "x" + destHeight + ", " + isClipped.ToString() + ")");

            /// 載入原始影像:
            Stream fs = null;
            LastError = String.Empty;

            try {  fs = File.Open(srcPathName, FileMode.Open, FileAccess.Read);  }
            catch (Exception ex) {  LastError = ex.Message; return false;  }

            MemoryStream ms = new MemoryStream();
            fs.CopyTo(ms);
            fs.Close();
            ms.Seek(0, SeekOrigin.Begin);

            Image srcImg = null;
            try {  srcImg = Bitmap.FromStream(ms);  }
            catch (Exception ex) {  LastError = ex.Message; return false;  }

            /// 準備一個 destWidth x destHeight 的畫布:
            Image destImg = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage(destImg);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);

            int x=0, y=0;
            int width = srcImg.Width;
            int height = srcImg.Height;

            if (isClipped == true)
            {
                /// 計算並比較兩影像的長寬比，並且進行裁切:
                Double srcAspect = ((double)srcImg.Width) / ((double)srcImg.Height);
                Double destAspect = ((double)destWidth) / ((double)destHeight);

                if (srcAspect > destAspect)
                {
                    width = (int)((float)height * destAspect);
                    x = (srcImg.Width - width) / 2;
                }
                else
                {
                    height = (int)((float)width / destAspect);
                    y = (srcImg.Height - height) / 2;
                }
            }

            /// 將 srcImg 描繪到 destImg 上:
            g.DrawImage(srcImg,
                new Rectangle(0, 0, destWidth, destHeight),
                new Rectangle(x, y, width, height),
                GraphicsUnit.Pixel);

            /// 將 destImg 儲存為 destPathName 檔案:
            ImageFormat destFormat = srcImg.RawFormat;
            String fileExt = Path.GetExtension(destPathName).ToLower();
            if ((fileExt.Equals(".jpg") == true) || (fileExt.Equals(".jpeg") == true)) {  destFormat = ImageFormat.Jpeg;  }
            else if (fileExt.Equals(".png") == true) {  destFormat = ImageFormat.Png;  }

            /// 如果 destPathName 路徑錯誤，這一行會發生 GDI+ 泛型錯誤，請檢查 exception log:
            try {  destImg.Save(destPathName, destFormat);  }
            catch (Exception ex) {  LastError = ex.Message; return false;  }

            return true;
        }
    }
}
