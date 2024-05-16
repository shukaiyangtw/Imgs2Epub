/** @file ChapterInfo.cs
 *  @brief 章節資訊

 *  這個程式用 ChapterInfo 和 Chapter 兩個類別來裝載章節資料，為了達到延遲載入、避免在程式一開始
 *  就載入所有的章節內容。在 ChapterInfo 類別當中只包括了章節標題、以及章節資料所在目錄的資訊，在
 *  Chapter 類別中包含 Paragraph 段落的集合、在 Paragraph 類別中包含 Photo 的集合。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/8/12 */

using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;

namespace Imgs2Epub
{
    public sealed class ChapterInfo : INotifyPropertyChanged
    {
        private AlbumInfo m_album;
        public AlbumInfo Album {  get {  return m_album;  }  }

        /// <summary>
        ///  這個資料成員指向章節的內容，初始為 null，需要編輯的時候才由 LoadXml() 載入之。
        /// </summary>
        public Chapter Body = null;

        /// <summary>
        ///  章節內容所在的目錄名稱。
        /// </summary>
        private String m_dir = "ch001";
        public String Directory { get {  return m_dir;  }  }
        private String m_chapFolder = null;
        public String Folder {  get {  return m_chapFolder;  }  }

        /// <summary>
        ///  章節標題。
        /// </summary>
        private String m_title = "(New Chapter)";
        public String Title
        {
            get {  return m_title;  }
            set {  m_title = value; OnPropertyChanged("Title");  }
        }

        /// <summary>
        ///  預先統計好的照片數量，因為想要在顯示章節清單的時候同時顯示該章之中的照片數量，又不希
        ///  望每次都必須載入 Body 並統計之，所以用一個資料成員去記它、必且寫入 XML 檔案當中，理
        ///  論上只要每次增減照片的時候都有確實更新這個值、並且寫入 XML 當中，它就會是正確的。
        /// </summary>
        private Int32 m_photoCount = 0;
        public Int32 PhotoCount
        {
            get {  return m_photoCount;  }
            set {  m_photoCount = value;  }
        }

        public bool RecountPhotos()
        {
            if (Body == null)
            {
                m_error = "The content of chapter is not loaded.";
                return false;
            }

            m_photoCount = 0;
            foreach (Paragraph para in Body.Paragraphs)
            {   m_photoCount += para.Photos.Count;  }

            return true;
        }

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
        ///  這個章節的內容是否被更動了？
        /// </summary>
        public Boolean IsModified = false;

        /// <summary>
        ///  傳回 LoadXml() 或 SaveXml() 所遇到的最後錯誤訊息。
        /// </summary>
        private String m_error = String.Empty;
        public String LastError {  get {  return m_error;  }  }

        public ChapterInfo(AlbumInfo album, String directory)
        {
            m_album = album;
            m_dir = directory;
            m_chapFolder = Path.Combine(m_album.ContentFolder, m_dir);
        }

        /// <summary>
        ///  將自己更名。
        /// </summary>
        public Boolean Rename(String newName)
        {
            Debug.WriteLine("ChapterInfo.Rename(" + m_dir + " to " + newName + ")");
            String fullPath = Path.Combine(m_album.ContentFolder, newName);
            if (System.IO.Directory.Exists(fullPath))
            {
                m_error = Properties.Resources.DirectoryAlreadyExists;
                Debug.WriteLine(m_error);
                return false;
            }

            /// 先嘗試刪除已生成的 .xhtml 檔案。
            try
            {
                String xhtmlPathName = Path.Combine(m_album.ContentFolder, m_dir + ".xhtml");
                if (File.Exists(xhtmlPathName)) {  File.Delete(xhtmlPathName); }
            }
            catch { }

            /// 將目錄更名:
            try
            {   System.IO.Directory.Move(m_chapFolder, fullPath);  }
            catch (Exception ex)
            {
                m_error = ex.Message;
                Debug.WriteLine(m_error);
                return false;
            }

            /// 目錄名稱已改變，套用新名稱：
            m_album.IsModified = true;
            m_chapFolder = fullPath;
            m_dir = newName;
            OnPropertyChanged("Directory");
            return true;
        }

        #region loading / writing chapter.xml.
        /// -----------------------------------------------------------------------------------

        /// <summary>
        ///  從 album directory/EPUB/m_dir/chapter.xml 讀取 Body。
        /// </summary>
        public Boolean LoadXml()
        {
            XmlDocument doc = new XmlDocument();
            String pathName = Path.Combine(m_chapFolder, "chapter.xml");
            Debug.WriteLine("ChapterInfo.LoadXml( " + pathName + " )...");

            if (File.Exists(pathName) == false)
            {   m_error = "File not found!";  return false;  }

            /// 嘗試解析 chapter.xml 為 XML DOM 資料:
            try {  doc.Load(pathName);  }
            catch (Exception ex)
            {
                doc = null;
                m_error = ex.Message;
                return false;
            }

            /// 釋放舊資料，並準備載入 Chapter Body:
            Body = null;
            Body = new Chapter(this);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = node as XmlElement;
                    if (element.Name.Equals("paragraph"))
                    {
                        Paragraph para = new Paragraph(this);
                        Body.Paragraphs.Add(para);

                        String s = element.GetAttribute("thumb_size");
                        if (String.IsNullOrEmpty(s) == false)
                        {   para.ThumbSize = Int32.Parse(s);  }

                        foreach (XmlNode child in element.ChildNodes)
                        {
                            if (child.NodeType == XmlNodeType.Element)
                            {
                                XmlElement ele = child as XmlElement;
                                if (ele.Name.Equals("title"))
                                {
                                    para.Title = ele.InnerText;
                                    Debug.WriteLine("sub-title: " + para.Title);
                                }
                                else if (ele.Name.Equals("context"))
                                {   para.Context = ele.InnerText;  }
                                else if (ele.Name.Equals("date"))
                                {
                                    Debug.WriteLine("date: " + ele.InnerText);
                                    if (ele.GetAttribute("visible").Equals("true"))
                                    {   para.DateIsVisible = true;  }

                                    try
                                    {
                                        DateTime dt = DateTime.Parse(ele.InnerText);
                                        para.Date = dt;
                                    }
                                    catch (Exception ex)
                                    {
                                        m_error = ex.Message;
                                        return false;
                                    }
                                }
                                else if (ele.Name.Equals("location"))
                                {
                                    para.Location = ele.InnerText;
                                    Debug.WriteLine("location: " + para.Location);

                                    if (ele.GetAttribute("visible").Equals("true"))
                                    {   para.LocationIsVisible = true;  }
                                }
                                else if (ele.Name.Equals("photo"))
                                {
                                    Photo photo = new Photo(this);
                                    photo.FileName = ele.GetAttribute("file");

                                    if (ele.GetAttribute("orient").Equals("portrait"))
                                    {   photo.Orient = Photo.Orientation.Portrait;  }
                                    else {  photo.Orient = Photo.Orientation.Landscape;  }

                                    s = ele.GetAttribute("thumb_size");
                                    if (String.IsNullOrEmpty(s) == false)
                                    {   photo.ThumbSize = Int32.Parse(s);  }

                                    photo.Description = ele.InnerText;
                                    para.Photos.Add(photo);
                                    Debug.WriteLine("photo: " + photo.FileName);
                                }
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
        ///  將這個 Info 和 Body 的內容儲存為 album directory/EPUB/m_dir/chapter.xml。
        /// </summary>
        public Boolean SaveXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(declaration);

            XmlElement root = doc.CreateElement("chapter");
            doc.AppendChild(root);

            foreach (Paragraph para in Body.Paragraphs)
            {
                XmlElement element = doc.CreateElement("paragraph");
                element.SetAttribute("thumb_size", para.ThumbSize.ToString());
                root.AppendChild(element);

                XmlElement child = doc.CreateElement("date");
                if (para.DateIsVisible) {  child.SetAttribute("visible", "true");  }
                else  {  child.SetAttribute("visible", "false");  }

                String str = para.Date.ToString("yyyy-MM-dd");
                XmlText tex = doc.CreateTextNode(str);
                child.AppendChild(tex);
                element.AppendChild(child);

                if (String.IsNullOrEmpty(para.Title) == false)
                {
                    child = doc.CreateElement("title");
                    tex = doc.CreateTextNode(para.Title);
                    child.AppendChild(tex);
                    element.AppendChild(child);
                }

                if (String.IsNullOrEmpty(para.Context) == false)
                {
                    child = doc.CreateElement("context");
                    tex = doc.CreateTextNode(para.Context);
                    child.AppendChild(tex);
                    element.AppendChild(child);
                }

                if (String.IsNullOrEmpty(para.Location) == false)
                {
                    child = doc.CreateElement("location");
                    if (para.LocationIsVisible) {  child.SetAttribute("visible", "true");  }
                    else {  child.SetAttribute("visible", "false");  }

                    tex = doc.CreateTextNode(para.Location);
                    child.AppendChild(tex);
                    element.AppendChild(child);
                }

                foreach (Photo photo in para.Photos)
                {
                    child = doc.CreateElement("photo");
                    child.SetAttribute("file", photo.FileName);

                    if (photo.Orient == Photo.Orientation.Landscape)
                    {   child.SetAttribute("orient", "landscape");  }
                    else
                    {   child.SetAttribute("orient", "portrait");  }

                    child.SetAttribute("thumb_size", photo.ThumbSize.ToString());

                    if (String.IsNullOrEmpty(photo.Description) == false)
                    {
                        tex = doc.CreateTextNode(photo.Description);
                        child.AppendChild(tex);
                    }

                    element.AppendChild(child);
                }
            }

            /// 把 XML document 寫入 album.xml:
            try
            {
                String pathName = Path.Combine(m_chapFolder, "chapter.xml");
                Debug.WriteLine("ChapterInfo.SaveXml( " + pathName + " )");

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
