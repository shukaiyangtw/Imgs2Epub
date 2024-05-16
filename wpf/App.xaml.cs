/** @file App.xaml.cs
 *  @brief 全域屬性

 *  在此宣告所有頁面共用的全域屬性，先在 OnStartup() 從 albums.xml 讀取目前的 AlbumInfo collection，
 *  並且覆寫 NavigationWindow_Closing() 結束時呼叫 SaveAlbums() 方法將相簿儲存回 albums.xml，檔案格式如下。

    [xml version=1.0 encoding=utf-8 >
    [albums>
        [album identifier=uuid dir=根目錄>
            <title>相簿標題</title>
            <firstdate>拍照日期第一天</firstdate>
            <lastdate>拍照日期最後一天</lastdate>
            <location>拍照地點</location>
            <author>拍攝者或相簿主角</author>
            <cover>封面檔名</cover>
            <filename>匯入前的檔名</filename>
        [/album>
    [/albums>

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2024/1/29 */

using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Imgs2Epub
{
    public partial class App : Application
    {
        /// 本程式的版本:
        private String m_myVer = String.Empty;
        public String ProgramVer { get { return m_myVer; } }

        /// 本程式的程式與資料根目錄:
        private String m_dataDir = String.Empty;
        public String DataDir { get { return m_dataDir; } }

        private String m_programDir = String.Empty;
        public String ProgramDir { get { return m_programDir; } }

        private String m_assetDir = String.Empty;
        public String AssetDir { get { return m_assetDir; } }

        /// <summary>
        ///  這個 info物件的集合只包含了最粗略的相簿標題、作者、地點、日期，不包含相簿內的照片與描述
        ///  文字資訊，這個集合用於在 ListView 中列出目前編輯中的相簿，使用者點選之後才前進 MainPage
        ///  並載入相簿內容 body，這個集合只須 OnStartup()當中載入一次，但是視 IsCollectionModified
        ///  是否為 true 可能會在 NavigationWindow_Closing() 以及其他地方儲存多次。
        /// </summary>
        public ObservableCollection<AlbumInfo> Albums = new ObservableCollection<AlbumInfo>();

        /// <summary>
        ///  目前選擇中的相簿物件，當使用者從 ListView 中選擇該本相簿，會前進 MainPage 並會載入它的
        ///  body，然後這個域物件被共用於諸多頁面之間。
        /// </summary>
        public AlbumInfo CurAlbum = null;

        /// <summary>
        ///  目前選擇中的章節，必然是 CurAlbum 當中的一章。
        /// </summary>
        public ChapterInfo CurChap = null;

        /// <summary>
        ///  目前編輯或檢視中的段落與照片，必然是 CurChap 的一部分。
        /// </summary>
        public Paragraph CurPara = null;
        public Photo CurPhoto = null;

        /// 執行時期選項:
        public Boolean ForcedEnUs = false;

        /// <summary>
        ///  程式剛啟動的時候載入使用者識別資料與既有統計資料，並且接受命令列參數。
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Debug.WriteLine("App.OnStartup()...");

            /// 取得目前的軟體版本和相關目錄:
            Assembly assembly = Assembly.GetExecutingAssembly();
            m_myVer = assembly.GetName().Version.ToString();
            FileInfo exeFileInfo = new FileInfo(assembly.Location);
            m_programDir = exeFileInfo.Directory.FullName;
            m_assetDir = Path.Combine(m_programDir, "Assets");
            Debug.WriteLine("Asset path: " + m_assetDir);

            /// 取得或建立資料根目錄:
            String localAppPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            m_dataDir = String.Format(@"{0}\AlbumEpubs", localAppPath);
            if (Directory.Exists(m_dataDir) == false) {  Directory.CreateDirectory(m_dataDir);  }
            Debug.WriteLine("Data path: " + m_dataDir);

            String thumbPath = Path.Combine(m_dataDir, "Thumbs");
            if (Directory.Exists(thumbPath) == false) {  Directory.CreateDirectory(thumbPath);  }

            /// 解析命令列參數，基於測試目的，可將整個程式切換成英文版:
            String[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; ++i)
            {
                if (args[i].Equals("-en"))
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                        new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
                    ForcedEnUs = true;
                }
            }

            #region 讀取 albums.xml...
            /// 先嘗試由 local folder 解讀 albums.xml，若檔案不存在或無法解讀則終止:
            String pathName = Path.Combine(m_dataDir, "albums.xml");
            if (File.Exists(pathName))
            {
                XmlDocument doc = new XmlDocument();
                Debug.WriteLine("Reading " + pathName + "...");
                try {  doc.Load(pathName);  }
                catch (Exception ex)
                {   doc = null;  MessageBox.Show(ex.Message);  }

                if (doc != null)
                {
                    /// 解讀 XmlDocument 並且建構 AlbumInfo 物件:
                    foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            XmlElement element = node as XmlElement;
                            if (element.Name.Equals("album"))
                            {
                                AlbumInfo info = new AlbumInfo(element.GetAttribute("dir"));
                                info.Identifier = element.GetAttribute("identifier");
                                Debug.WriteLine("album " + info.Directory + ": id=" + info.Identifier);

                                foreach (XmlNode child in element.ChildNodes)
                                {
                                    if (child.NodeType == XmlNodeType.Element)
                                    {
                                        XmlElement ele = child as XmlElement;
                                        if (ele.Name.Equals("title"))         {  info.Title = ele.InnerText;  }
                                        else if (ele.Name.Equals("location")) {  info.Location = ele.InnerText;  }
                                        else if (ele.Name.Equals("author"))   {  info.Author = ele.InnerText;  }
                                        else if (ele.Name.Equals("cover"))    {  info.CoverFile = ele.InnerText;  }
                                        else if (ele.Name.Equals("firstdate"))
                                        {
                                            try
                                            {
                                                DateTime dt = DateTime.Parse(ele.InnerText);
                                                info.FirstDate = dt;
                                            }
                                            catch (Exception ex)
                                            {   MessageBox.Show(ex.Message);  }
                                        }
                                        else if (ele.Name.Equals("lastdate"))
                                        {
                                            try
                                            {
                                                DateTime dt = DateTime.Parse(ele.InnerText);
                                                info.LastDate = dt;
                                            }
                                            catch (Exception ex)
                                            {   MessageBox.Show(ex.Message);  }
                                        }
                                        else if (ele.Name.Equals("filename"))
                                        {
                                            info.FileName = ele.InnerText;
                                            Debug.WriteLine("filename: " + info.FileName);
                                        }
                                    }
                                }

                                /// 把 album info 加入到全域的 collection 當中:
                                Albums.Add(info);
                            }
                        }
                    }
                }
            }
            else
            {   Debug.WriteLine("albums.xml not found!");  }
            #endregion

            AlbumInfo.IsCollectionModified = false;
        }

        #region 儲存 albums.xml...
        /// ----------------------------------------------------------------------------------------

        /// <summary>
        ///  將 Albums 儲存為 albums.xml。
        /// </summary>
        public void SaveAlbumsXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(declaration);

            XmlElement root = doc.CreateElement("albums");
            doc.AppendChild(root);

            foreach (AlbumInfo info in Albums)
            {
                /// 為每一本相簿建立 album 元素，並加入倒 xml document 的根節點:
                XmlElement element = doc.CreateElement("album");
                element.SetAttribute("identifier", info.Identifier);
                element.SetAttribute("dir", info.Directory);

                XmlElement child = doc.CreateElement("title");
                XmlText text = doc.CreateTextNode(info.Title);
                child.AppendChild(text);
                element.AppendChild(child);

                child = doc.CreateElement("firstdate");
                /* String str = info.FirstDate.ToString("yyyy-MM-dd HH:mm:ss"); */
                String str = info.FirstDate.ToString("yyyy-MM-dd");
                text = doc.CreateTextNode(str);
                child.AppendChild(text);
                element.AppendChild(child);

                if (info.LastDate.Equals(info.FirstDate) == false)
                {
                    child = doc.CreateElement("lastdate");
                    /* str = info.LastDate.ToString("yyyy-MM-dd HH:mm:ss"); */
                    str = info.LastDate.ToString("yyyy-MM-dd");
                    text = doc.CreateTextNode(str);
                    child.AppendChild(text);
                    element.AppendChild(child);
                }

                if (String.IsNullOrWhiteSpace(info.Location) == false)
                {
                    child = doc.CreateElement("location");
                    text = doc.CreateTextNode(info.Location);
                    child.AppendChild(text);
                    element.AppendChild(child);
                }

                if (String.IsNullOrWhiteSpace(info.Author) == false)
                {
                    child = doc.CreateElement("author");
                    text = doc.CreateTextNode(info.Author);
                    child.AppendChild(text);
                    element.AppendChild(child);
                }

                if (String.IsNullOrWhiteSpace(info.CoverFile) == false)
                {
                    child = doc.CreateElement("cover");
                    text = doc.CreateTextNode(info.CoverFile);
                    child.AppendChild(text);
                    element.AppendChild(child);
                }

                if (String.IsNullOrEmpty(info.FileName) == false)
                {
                    child = doc.CreateElement("filename");
                    XmlText tex = doc.CreateTextNode(info.FileName);
                    child.AppendChild(tex);
                    element.AppendChild(child);
                }

                root.AppendChild(element);
            }

            Debug.WriteLine("App.SaveAlbumsXml():");

            /// 把 XML document 寫入 albums.xml:
            String pathName = Path.Combine(m_dataDir, "albums.xml");

            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                FileStream stream = File.Create(pathName);
                XmlWriter writer = XmlWriter.Create(stream, settings);
                doc.Save(writer);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
                return;
            }

            AlbumInfo.IsCollectionModified = false;
            Debug.WriteLine("Done.");
        }
        #endregion
    }
}
