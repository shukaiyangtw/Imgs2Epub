/** @file AlbumInfo.cs
 *  @brief 相簿基本資訊

 *  這個程式用 AlbumInfo 和 Album 兩個類別來裝載相簿資料，為了達到延遲載入、避免在程式一開始就載入所
 *  有相簿內容。其中 AlbumInfo 僅包含標題、作者、日期、封面圖與存放路徑等這些基本資訊，用於 MainPage
 *  的相簿列表展示，當使用者點擊了列表中的某一本相簿並進入 AlbumPage 編輯它的時候，才載入 Body 內容，
 *  Album 類別才是相簿資料的主要聚合體，包含了相簿的編輯資訊以及照片章節。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/12/5 */

using System;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace Imgs2Epub
{
    public sealed class AlbumInfo : INotifyPropertyChanged
    {
        /// <summary>
        ///  這個資料成員指向相簿的內容，初始為 null，需要編輯的時候才由 LoadXml() 載入之。
        /// </summary>
        public Album Body = null;

        /// <summary>
        ///  相簿的唯一識別碼，通常是個字串化的 UUID。
        /// </summary>
        public String Identifier = "(Unidentified)";

        /// <summary>
        ///  這個相簿內容所在的目錄名稱。
        /// </summary>
        private String m_dir = String.Empty;
        public String Directory {  get {  return m_dir;  }  }

        private String m_epubFolder = null;
        public String ContentFolder {  get {  return m_epubFolder;  }  }

        /// <summary>
        ///  相簿封面檔案名稱。
        /// </summary>
        public String CoverFile {  set;  get;  }

        /// <summary>
        ///  相簿被匯入前的原始檔名。
        /// </summary>
        public String FileName = String.Empty;

        #region bindable properties: Title, Author, Location, FirstDate, LastDate.
        /// ///////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  相簿標題。
        /// </summary>
        private String m_title = "(Untitled)";
        public String Title
        {
            get {  return m_title;  }
            set {  m_title = value;  }
        }

        /// <summary>
        ///  相簿作者。
        /// </summary>
        private String m_author = String.Empty;
        public String Author
        {
            get {  return m_author;  }
            set {  m_author = value;  }
        }

        /// <summary>
        ///  拍攝地點。
        /// </summary>
        private String m_location = String.Empty;
        public String Location
        {
            get {  return m_location;  }
            set {  m_location = value;  }
        }

        /// <summary>
        ///  拍照日期第一天與最後一天。
        /// </summary>
        private DateTime m_firstDate;
        private DateTime m_lastDate;

        public DateTime FirstDate
        {
            get {  return m_firstDate;  }
            set
            {
                m_firstDate = value;
                m_lastDate = m_firstDate;
            }
        }

        public DateTime LastDate
        {
            get {  return m_lastDate;  }
            set {  m_lastDate = value;  }
        }
        #endregion

        #region read-only properties: CoverImagePath, ThumbPath, DateStr.
        /// ///////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  傳回封面圖檔的物件。
        /// </summary>
        public BitmapImage CoverImageSrc
        {
            get
            {
                App app = Application.Current as App;

                if (String.IsNullOrEmpty(CoverFile) == false)
                {
                    /// 嘗試傳回 LocalApplicationData/AlbumEpubs/m_dir/EPUB/coverFile:
                    String[] tokens = new String[] {  app.DataDir, m_dir, "EPUB", CoverFile  };
                    String pathName = Path.Combine(tokens);
                    if (File.Exists(pathName))
                    {
                        BitmapImage bmp = ImagingHelper.LoadImageFile(pathName);
                        if (bmp != null) {  return bmp;  }
                    }
                }

                /// 如果 coverFile 是空字串或檔案不存在，傳回 Assets 中的預設檔案:
                String assetPathName = Path.Combine(app.AssetDir, "cover_none.jpg");
                return ImagingHelper.LoadImageFile(assetPathName);
            }
        }

        /// <summary>
        ///  傳回封面縮圖物件，預設是 Thumbs 目錄下的同名 JPEG 檔案，若不存在則傳回 CoverFile。
        /// </summary>
        public BitmapImage ThumbImageSrc
        {
            get
            {
                /// 傳回 LocalApplicationData/AlbumEpubs/Thumbs/m_dir.jpg:
                App app = Application.Current as App;
                String[] tokens = new String[] {  app.DataDir, "Thumbs", m_dir + ".jpg"  };
                String pathName = Path.Combine(tokens);
                if (File.Exists(pathName))
                {
                    BitmapImage bmp = ImagingHelper.LoadImageFile(pathName);
                    if (bmp != null) {  return bmp;  }
                }

                /// 沒有 Thumbs 目錄下的檔案，嘗試傳回 CoverFile:
                if (String.IsNullOrEmpty(CoverFile) == false)
                {
                    tokens = new String[] {  app.DataDir, m_dir, "EPUB", CoverFile  };
                    pathName = Path.Combine(tokens);
                    if (File.Exists(pathName))
                    {
                        BitmapImage bmp = ImagingHelper.LoadImageFile(pathName);
                        if (bmp != null) {  return bmp;  }
                    }
                }

                /// 如果檔案不存在則傳回 Assets 中的預設檔案:
                String assetPathName = Path.Combine(app.AssetDir, "cover_thumb.jpg");
                return ImagingHelper.LoadImageFile(assetPathName);
            }
        }

        /// <summary>
        ///  統一的日期字串顯示格式。 
        /// </summary>
        static public readonly String DateFormat = "MMM dd, yyyy";

        /// <summary>
        ///  用於顯示相簿日期的唯讀屬性。
        /// </summary>
     /* public String FirstDateStr
        {   get {  return m_firstDate.ToString(DateFormat);  }  } */

        public String DateStr
        {
            get
            {
                String str1 = m_firstDate.ToString(DateFormat);
                if (m_firstDate.Date.Equals(m_lastDate.Date)) {  return str1;  }

                String str2 = m_lastDate.ToString(DateFormat);
                return str1 + " - " + str2;
            }
        }

        /* 統計照片數量:
        public Int32 PhotoCount
        {
            get
            {
                int photoCount = 0;

                if (Body == null)
                {   m_error = "The content of album is not loaded.";  return -1;  }

                foreach (ChapterInfo info in Body.Chapters)
                {   photoCount += info.PhotoCount;  }

                return photoCount;
            }
        } */
        #endregion

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
        ///  這個相簿資訊的內容是否被更動了？
        /// </summary>
        public Boolean IsModified = false;

        /// <summary>
        ///  只要有任何一個 AlbumInfo 的內容被修改了，就應該令它為 true，以通知程式儲存 albums.xml 檔案。
        /// </summary>
        static public Boolean IsCollectionModified = false;

        /// <summary>
        ///  傳回 LoadXml() 或 SaveXml() 所遇到的最後錯誤訊息。
        /// </summary>
        private String m_error = String.Empty;
        public String LastError
        {
            get {  return m_error;  }
            set {  m_error = value;  }
        }

        /// <summary>
        ///  初始化所有的成員，但是不包括產生 UUID。
        /// </summary>
        public AlbumInfo(String directory)
        {
            m_dir = directory;
            m_firstDate = DateTime.Now.Date;
            m_lastDate = m_firstDate;

            /// 取得所在路徑:
            App app = Application.Current as App;
            String[] tokens = new String[] {  app.DataDir, m_dir, "EPUB"  };
            m_epubFolder = Path.Combine(tokens);
        }

        #region loading/writing album.xml.
        /// -----------------------------------------------------------------------------------

        /// <summary>
        ///  從 Directory/EPUB/album.xml 讀取 Body。
        /// </summary>
        public Boolean LoadXml()
        {
            XmlDocument doc = new XmlDocument();
            String pathName = Path.Combine(m_epubFolder, "album.xml");
            Debug.WriteLine("AlbumInfo. LoadXml( " + pathName + " )...");

            if (File.Exists(pathName) == false)
            {   m_error = "File not found!";  return false;  }

            /// 嘗試解析 album.xml 為 XML DOM 資料:
            try {  doc.Load(pathName);  }
            catch (Exception ex)
            {
                doc = null;
                m_error = ex.Message;
                return false;
            }

            /// 釋放舊資料，並準備載入 Album Body:
            Body = null;
            Body = new Album(this);
            Identifier = doc.DocumentElement.GetAttribute("identifier");
            Debug.WriteLine("identifier: " + Identifier);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = node as XmlElement;
                    if (element.Name.Equals("title"))
                    {
                        m_title = element.InnerText;
                        Debug.WriteLine("title: " + m_title);
                    }
                    else if (element.Name.Equals("firstdate"))
                    {
                        Debug.WriteLine("firstdate: " + element.InnerText);

                        try
                        {
                            DateTime dt = DateTime.Parse(element.InnerText);
                            m_firstDate = dt;
                            m_lastDate = m_firstDate;
                        }
                        catch (Exception ex)
                        {
                            m_error = ex.Message;
                            return false;
                        }
                    }
                    else if (element.Name.Equals("lastdate"))
                    {
                        Debug.WriteLine("lastdate: " + element.InnerText);

                        try
                        {
                            DateTime dt = DateTime.Parse(element.InnerText);
                            m_lastDate = dt;
                        }
                        catch (Exception ex)
                        {
                            m_error = ex.Message;
                            return false;
                        }
                    }
                    else if (element.Name.Equals("location"))
                    {
                        m_location = element.InnerText;
                        Debug.WriteLine("location: " + m_location);
                    }
                    else if (element.Name.Equals("author"))
                    {
                        m_author = element.InnerText;
                        Debug.WriteLine("author: " + m_author);
                    }
                    else if (element.Name.Equals("text"))
                    {
                        Body.Text = element.InnerText;
                    }
                    else if (element.Name.Equals("cover"))
                    {
                        CoverFile = element.InnerText;
                        Debug.WriteLine("cover: " + CoverFile);
                    }
                    else if (element.Name.Equals("cover_editor"))
                    {
                        Body.Cover.ReadXmlElement(element);
                        Debug.WriteLine("cover settings is parsed.");
                    }
                    else if (element.Name.Equals("chapters"))
                    {
                        /* 相簿章節資訊
                        <chapters>
                            <chapter dir="章節路徑(子目錄名稱)" photos="照片數量">章節標題</chapter>
                        </ chapters > */
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            if (child.NodeType == XmlNodeType.Element)
                            {
                                XmlElement ele = child as XmlElement;
                                ChapterInfo chapter = new ChapterInfo(this, ele.GetAttribute("dir"));

                                String str = ele.GetAttribute("photos");
                                if (String.IsNullOrEmpty(str) == false)
                                {   chapter.PhotoCount = Int32.Parse(str);  }

                                chapter.Title = ele.InnerText;
                                Debug.WriteLine("chapter \"" + chapter.Directory + "\": " + chapter.Title);
                                Body.Chapters.Add(chapter);
                            }
                        }
                    }
                }
            }

            IsModified = false;
            return true;
        }

        /// -----------------------------------------------------------------------------------

        /// <summary>
        ///  將這個 Info 和 Body 的內容儲存為 root/EPUB/album.xml。
        /// </summary>
        public Boolean SaveXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(declaration);

            XmlElement root = doc.CreateElement("album");
            root.SetAttribute("identifier", Identifier);
            doc.AppendChild(root);

            if (String.IsNullOrEmpty(m_title) == false)
            {
                XmlElement child = doc.CreateElement("title");
                XmlText tex = doc.CreateTextNode(m_title);
                child.AppendChild(tex);
                root.AppendChild(child);
            }

            XmlElement element = doc.CreateElement("firstdate");
         /* String str = m_firstDate.ToString("yyyy-MM-dd HH:mm:ss"); */
            String str = m_firstDate.ToString("yyyy-MM-dd");
            XmlText text = doc.CreateTextNode(str);
            element.AppendChild(text);
            root.AppendChild(element);

            if (m_firstDate.Equals(m_lastDate) == false)
            {
                XmlElement child = doc.CreateElement("lastdate");
             /* str = m_lastDate.ToString("yyyy-MM-dd HH:mm:ss"); */
                str = m_lastDate.ToString("yyyy-MM-dd");
                XmlText tex = doc.CreateTextNode(str);
                child.AppendChild(tex);
                root.AppendChild(child);
            }

            if (String.IsNullOrEmpty(m_location) == false)
            {
                XmlElement child = doc.CreateElement("location");
                XmlText tex = doc.CreateTextNode(m_location);
                child.AppendChild(tex);
                root.AppendChild(child);
            }

            if (String.IsNullOrEmpty(m_author) == false)
            {
                XmlElement child = doc.CreateElement("author");
                XmlText tex = doc.CreateTextNode(m_author);
                child.AppendChild(tex);
                root.AppendChild(child);
            }

            if (String.IsNullOrEmpty(Body.Text) == false)
            {
                XmlElement child = doc.CreateElement("text");
                XmlText tex = doc.CreateTextNode(Body.Text);
                child.AppendChild(tex);
                root.AppendChild(child);
            }

            if (String.IsNullOrEmpty(CoverFile) == false)
            {
                XmlElement child = doc.CreateElement("cover");
                XmlText tex = doc.CreateTextNode(CoverFile);
                child.AppendChild(tex);
                root.AppendChild(child);
            }

            /* 編輯相簿封面用的資訊: */
            root.AppendChild(Body.Cover.CreateXmlElement(doc));

            /* 相簿章節資訊
            <chapters>
                <chapter dir="章節路徑(子目錄名稱)" photos="照片數量">章節標題</chapter>
            </ chapters > */
            element = doc.CreateElement("chapters");
            root.AppendChild(element);

            foreach (ChapterInfo chap in Body.Chapters)
            {
                if ((chap.IsModified) && (chap.Body != null))
                {
                    if (chap.SaveXml() == false)
                    {   m_error = chap.LastError;  return false;  }
                }

                XmlElement child = doc.CreateElement("chapter");
                child.SetAttribute("dir", chap.Directory);

                if (chap.PhotoCount > 0)
                {   child.SetAttribute("photos", chap.PhotoCount.ToString());  }

                if (String.IsNullOrEmpty(chap.Title) == false)
                {
                    XmlText tex = doc.CreateTextNode(chap.Title);
                    child.AppendChild(tex);
                }

                element.AppendChild(child);
            }

            /// 把 XML document 寫入 album.xml:
            try
            {
                String pathName = Path.Combine(m_epubFolder, "album.xml");
                Debug.WriteLine("AlbumInfo.SaveXml( " + pathName + " )");

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                FileStream stream = File.Create(pathName);
                XmlWriter writer = XmlWriter.Create(stream, settings);
                doc.Save(writer);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                m_error = ex.Message;
                return false;
            }

            IsModified = false;
            return true;
        }
        #endregion
    }
}
