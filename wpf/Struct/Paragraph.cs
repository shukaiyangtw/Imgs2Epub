/** @file Paragraph.cs
 *  @brief 章節段落

 *  段落可視為一些相同時間地點的照片集合，這個類別包含一段文字，以及選擇性的副標題、日期、時間。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/9/18 */

using System;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Imgs2Epub
{
    public sealed class Paragraph : INotifyPropertyChanged
    {
        private ChapterInfo m_info;
        public ChapterInfo Chapter {  get {  return m_info;  }  }
        public int ThumbSize {  get;  set;  }

        /// <summary>
        ///  段落標題。
        /// </summary>
        private String m_title = String.Empty;
        public String Title
        {
            get {  return m_title;  }
            set {  m_title = value;  OnPropertyChanged("Title");  }
        }

        /// <summary>
        ///  標註地點。
        /// </summary>
        public String Location {  get; set;  }
        public Boolean LocationIsVisible {  get;  set;  }

        /// <summary>
        ///  標註拍照日期。
        /// </summary>
        public DateTime Date {  get; set;  }
        public Boolean DateIsVisible {  get; set;  }

        public String FirstDate
        {
            get
            {
                if ((DateIsVisible == true) && (Date != null))
                {   return Date.ToString("d");  }
                return String.Empty;
            }
        }

        /// <summary>
        ///  段落文字內容。
        /// </summary>
        public String Context {  get; set;  }

        /// <summary>
        ///  本段落的照片集。
        /// </summary>
        public ObservableCollection<Photo> Photos = new ObservableCollection<Photo>();

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
        ///  載入段落中第一張照片的縮圖，若不存在則載入 jpeg.png 並傳回之。
        /// </summary>
        public BitmapImage ThumbImageSrc
        {
            get
            {
                if (Photos.Count > 0) {  return Photos[0].ThumbImageSrc;  }

                /// 如果第一張圖片的縮圖檔案不存在，傳回 Assets 中的預設檔案:
                App app = Application.Current as App;
                String assetPathName = Path.Combine(app.AssetDir, "jpeg.png");
                return ImagingHelper.LoadImageFile(assetPathName);
            }
        }

        public Paragraph(ChapterInfo info)
        {
            m_info = info;
            if (info.Album != null) {  Date = info.Album.FirstDate;  }
            else {  Date = DateTime.Now.Date;  }
            DateIsVisible = false;
            LocationIsVisible = false;

            ThumbSize = 4;
            Location = "";
            Context = "";
        }

        /// <summary>
        ///  根據目前的照片數量，重新評估段落的縮圖尺寸。
        /// </summary>
        public void EvaluateTempSize()
        {
            int blocks = 0;
            foreach (Photo photo in Photos)
            {
                if (photo.Orient == Photo.Orientation.Landscape)
                {   blocks += 2;  } else {  ++blocks;  }
            }

            int thumbSize = 4;
            /// 預計畫面寬度可以裝 8 個寬度單位，如果 blocks 為 2，可以裝兩個 4X 的縮圖:
            if (blocks < 3) {  thumbSize = 4;  }
            /* 如果 blocks 為 3，可以裝兩個 3X 的縮圖:
            else if (blocks == 3) {  thumbSize = 3;  } */
            /// 如果 blocks 為 4，可以裝兩個 2X 的縮圖:
            else if (blocks < 5) {  thumbSize = 2;  }
            /// 如果更多 blocks，縮圖就一律是 1X:
            else {  thumbSize = 1;  }

            ThumbSize = thumbSize;
        }
    }
}
