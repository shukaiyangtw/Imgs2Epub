/** @file Photo.cs
 *  @brief 照片檔名與說明

 *  包含照片的檔案名稱以及單行說明文字。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/9/15 */

using System;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Imgs2Epub
{
    public sealed class Photo : INotifyPropertyChanged
    {
        public enum Orientation
        {   Landscape = 0, Portrait  }

        /// <summary>
        ///  此照片屬於哪一個章節。
        /// </summary>
        private ChapterInfo m_info;
        public ChapterInfo Chapter {  get {  return m_info;  }  }

        /// <summary>
        ///  照片是直式還是橫式？
        /// </summary>
        public Orientation Orient = Orientation.Landscape;

        /// <summary>
        ///  此照片目前的縮圖尺寸，藉以判斷是否與 Paragraph 相同(需要重新產製否？)。
        /// </summary>
        public int ThumbSize = 4;

        /// <summary>
        /// 照片檔案名稱。
        /// </summary>
        public String FileName = String.Empty;

        /// <summary>
        ///  實作 INotifyPropertyChanged 介面。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {   handler(this, new PropertyChangedEventArgs(propertyName));  }
        }

        /// <summary>
        ///  照片說明。
        /// </summary>
        private String m_text = String.Empty;
        public String Description
        {
            get {  return m_text;  }
            set {  m_text = value; OnPropertyChanged("Description");  }
        }

        public Photo(ChapterInfo info)
        {
            m_info = info;
            Description = "";
        }

        /// <summary>
        ///  載入照片圖檔，若不存在則載入 jpeg.png 並傳回之。
        /// </summary>
        public BitmapImage ImageSrc
        {
            get
            {
                if (String.IsNullOrEmpty(FileName) == false)
                {
                    /// 嘗試傳回 app.DataDir/(album)/EPUB/(chap)/(FileName):
                    String pathName = Path.Combine(m_info.Folder, FileName);
                    if (File.Exists(pathName))
                    {
                        BitmapImage bmp = ImagingHelper.LoadImageFile(pathName);
                        if (bmp != null) {  return bmp;  }
                    }
                }

                /// 如果圖片檔案不存在，傳回 Assets 中的預設檔案:
                App app = Application.Current as App;
                String assetPathName = Path.Combine(app.AssetDir, "jpeg.png");
                return ImagingHelper.LoadImageFile(assetPathName);
            }
        }

        /// <summary>
        ///  載入照片的縮圖，若不存在則載入 jpeg.png 並傳回之。
        /// </summary>
        public BitmapImage ThumbImageSrc
        {
            get
            {
                if (String.IsNullOrEmpty(FileName) == false)
                {
                    /// 嘗試傳回 app.DataDir/(album)/EPUB/(chap)/thumbs/(FileName):
                    String[] tokens = new String[] {  m_info.Folder, "thumbs", FileName  };
                    String pathName = Path.Combine(tokens);
                    if (File.Exists(pathName))
                    {
                        BitmapImage bmp = ImagingHelper.LoadImageFile(pathName);
                        if (bmp != null) {  return bmp;  }
                    }
                }

                /// 如果縮圖檔案不存在，傳回 Assets 中的預設檔案:
                App app = Application.Current as App;
                String assetPathName = Path.Combine(app.AssetDir, "jpeg.png");
                return ImagingHelper.LoadImageFile(assetPathName);
            }
        }
    }
}
