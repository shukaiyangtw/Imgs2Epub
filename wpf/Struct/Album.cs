/** @file Album.cs
 *  @brief 相簿內容

 *  這個程式用 AlbumInfo 和 Album 兩個類別來裝載相簿資料，為了達到延遲載入、避免在程式一開始就載入所
 *  有相簿內容。其中 AlbumInfo 僅包含標題、作者、日期、封面圖與存放路徑等這些基本資訊，用於 MainPage
 *  的相簿列表展示，當使用者點擊了列表中的某一本相簿並進入 AlbumPage 編輯它的時候，才載入 Body 內容，
 *  Album 類別才是相簿資料的主要聚合體，包含了相簿的編輯資訊以及照片章節。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/8/12 */

using System;
using System.IO;
using System.Collections.ObjectModel;
using Imgs2Epub.Properties;

namespace Imgs2Epub
{
    public sealed class Album
    {
        /// <summary>
        ///  指向相簿的標題作者與日期資訊
        /// </summary>
        private AlbumInfo m_info;
        public AlbumInfo Header { get { return m_info; } }

        /// <summary>。
        ///  這個 chapter info 的集合只包含章節的標題，用於管理目錄(table of contents)。
        /// </summary>
        public ObservableCollection<ChapterInfo> Chapters = new ObservableCollection<ChapterInfo>();

        /// <summary>
        ///  相簿簡介文字。
        /// </summary>
        private String m_text = String.Empty;
        public String Text
        {
            get {  return m_text;  }
            set {  m_text = value;   }
        }

        /// <summary>
        ///  關於封面影像的編輯資訊。
        /// </summary>
        public CoverSettings Cover = new CoverSettings();

        /// <summary>
        ///  初始化所有的成員，並建立 info 與 body 之間的連結。
        /// </summary>
        public Album(AlbumInfo info)
        {
            m_info = info;
            info.Body = this;
        }

        /// <summary>
        ///  這個函式在目前的 album 目錄之下新增一個 chXXX 子目錄，並在當中產生預設的 chapter.xml。
        /// </summary>
        public ChapterInfo CreateChapter()
        {
            /// 先找到第一個不存在的 chXXX 編號目錄:
            Int32 sn = Chapters.Count + 1;
            String dir = "ch" + sn.ToString("D3");
            String epubPath = m_info.ContentFolder;
            String pathName = Path.Combine(epubPath, dir);

            while (Directory.Exists(pathName))
            {
                ++sn;
                dir = "ch" + sn.ToString("D3");
                pathName = Path.Combine(epubPath, dir);
            }

            /// 建立這個目錄，並且產生 ChapterInfo 與 Chapter 物件:
            try
            {   Directory.CreateDirectory(pathName);  }
            catch (Exception ex)
            {   m_info.LastError = ex.Message; return null;  }

            ChapterInfo chapInfo = new ChapterInfo(m_info, dir);
            chapInfo.Title = Resources.NewChapTitle;
            chapInfo.Body = new Chapter(chapInfo);

            /* 在這個新章節裡面產生第一個新的空白段落:
            Paragraph para = new Paragraph(chapInfo);
            para.Date = m_info.FirstDate;
            chapInfo.Body.Paragraphs.Add(para); */

            /// 利用 ChapterInfo 的 SaveXml() 產生初始的 chapters.xml 檔案:
            if (chapInfo.SaveXml() == false)
            {   m_info.LastError = chapInfo.LastError;  return null;  }

            /// 將這個新建立的 ChapterInfo 加入到 Chapters 當中:
            Chapters.Add(chapInfo);
            m_info.IsModified = true;
            return chapInfo;
        }
    }
}
