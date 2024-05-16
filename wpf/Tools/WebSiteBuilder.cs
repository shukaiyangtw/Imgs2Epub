/** @file WebSiteBuilder.cs
 *  @brief 章節資訊

 *  這個整合的類別會檢查相簿目錄下所有的 .xml 與 .xhtml 檔案版本，並且視需要使用對應的 XhtmlBuilder
    類別重新產生檔案。通常 .xhtml 都是根據 .xml 的內容所產生的，所以如果 .xhtml 檔案不存在，或是檔案
    時間比 .xml 舊，就必須載入該 album.xml 或 chapter.xml 檔案到記憶體，並重新產生 .xhtml 檔案。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/8/14 */

using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace Imgs2Epub
{
    class WebSiteBuilder
    {
        /// <summary>
        ///  因為要 build 的相簿不一定是 app.CurAlbum，所以在建構式輸入物件。
        /// </summary>
        private AlbumInfo m_album;

        // <summary>
        ///  偵錯訊息。
        /// </summary>
        private String m_lastError = String.Empty;
        public String LastError {  get {  return m_lastError;  }  }

        public WebSiteBuilder(AlbumInfo album)
        {   m_album = album;  }

        /// <summary>
        ///  這個函式首先檢查 m_album 每個章節的 .xhtml 檔案是否為最新，若不是則重建之，接著檢查
        ///  title.xhtml, toc.xhtml, index.html 是否需要重建。
        /// </summary> 
        public Boolean Build()
        {
            String fileName;
            Debug.WriteLine("WebSiteBuilder.Build(" + m_album.Directory + ")...");

            /// 視需要從 album.xml 載入相簿章節，已取得 Chapters 集合:
            if (m_album.Body == null)
            {
                if (m_album.LoadXml() == false)
                {
                    m_lastError = m_album.LastError;
                    Debug.WriteLine(m_lastError);
                    return false;
                }
            }

            /// 對於集合內的每個 chapter，檢查其 .xhtml 檔案是否較舊或是不存在，嘗試重建檔案
            foreach (ChapterInfo chapter in m_album.Body.Chapters)
            {
                fileName = chapter.Directory + ".xhtml";
                String xhtmlPathName = Path.Combine(m_album.ContentFolder, chapter.Directory + ".xhtml");
                String xmlPathName = Path.Combine(chapter.Folder, "chapter.xml");
                if (DiskHelper.IsFileNewerThan(xhtmlPathName, xmlPathName) == false)
                {
                    if (BuildChapter(chapter) == false)
                    {   Debug.WriteLine(m_lastError);  return false;   }
                }
            }

            /// 依序檢查 title.xhtml, toc.xhtml, index.html 是否需要重建:
            if (DiskHelper.IsNewerThan(m_album.ContentFolder, "title.xhtml", "album.xml") == false)
            {
                Debug.WriteLine("Generating title.xhtml...");
                String xhtmlPathName = Path.Combine(m_album.ContentFolder, "title.xhtml");
                 TitleXhtmlBuilder builder1 = new TitleXhtmlBuilder(m_album);
                if (builder1.ExportToFile(xhtmlPathName) == false)
                {   m_lastError = builder1.LastError;  Debug.WriteLine(m_lastError);  return false;  }

                Debug.WriteLine("Generating toc.xhtml...");
                String tocPathName = Path.Combine(m_album.ContentFolder, "toc.xhtml");
                TocXhtmlBuilder builder2 = new TocXhtmlBuilder(m_album);
                if (builder2.ExportToFile(tocPathName) == false)
                {   m_lastError = builder2.LastError;  Debug.WriteLine(m_lastError);  return false;  }

                Debug.WriteLine("Generating index.html...");
                xhtmlPathName = Path.Combine(m_album.ContentFolder, "index.html");
                IndexHtmlBuilder builder3 = new IndexHtmlBuilder(m_album);
                if (builder3.ExportToFile(xhtmlPathName) == false)
                {   m_lastError = builder3.LastError;  Debug.WriteLine(m_lastError);  return false;  }
            }

            return true;
        }

        /// ---------------------------------------------------------------------------------------

        /// <summary>
        ///  這個函式重建 m_album 的章節 chapter 之 .xhtml 檔案、包括照片的瀏覽網頁。
        /// </summary> 
        public Boolean BuildChapter(ChapterInfo chapter)
        {
            String fileName = String.Empty;
            String xhtmlPathName = String.Empty;
            Debug.WriteLine("WebSiteBuilder.BuildChapter(" + chapter.Directory + ")...");

            /// 載入 chapter 的內容，以便檢查每一張照片的 .wj6.xhtml 是否已產生。
            if (chapter.Body == null)
            {
                if (chapter.LoadXml() == false)
                {
                    m_lastError = chapter.LastError;
                    Debug.WriteLine(m_lastError);
                    return false;
                }
            }

            foreach (Paragraph para in chapter.Body.Paragraphs)
            {
                foreach (Photo photo in para.Photos)
                {
                    /// 因為圖片檔案都是加入相簿以後就不再更動的，所以沒有檔案版本的問題，只須檢查
                    /// .wj6.xhtml 是否存在，若不存在則重新產生之。
                    fileName = Path.GetFileNameWithoutExtension(photo.FileName) + ".wj6.xhtml";
                    xhtmlPathName = Path.Combine(chapter.Folder, fileName);
                    if (File.Exists(xhtmlPathName) == false)
                    {
                        /// 重新產生 (album)/(chapter)/(photofile).wj6.xhtml:
                        Debug.WriteLine("Generating " + fileName + "...");
                        PhotoXhtmlBuilder photoBuilder = new PhotoXhtmlBuilder(photo);
                        if (photoBuilder.ExportToFile(xhtmlPathName) == false)
                        {   m_lastError = photoBuilder.LastError;  Debug.WriteLine(m_lastError);  return false;  }
                    }

                }
            }

            /// 重新產生 (album)/(chapter).xhtml:
            fileName = chapter.Directory + ".xhtml";
            Debug.WriteLine("Generating " + fileName + "...");
            xhtmlPathName = Path.Combine(m_album.ContentFolder, fileName);
            ChapterXhtmlBuilder builder = new ChapterXhtmlBuilder(chapter);
            if (builder.ExportToFile(xhtmlPathName) == false)
            {   m_lastError = builder.LastError;  Debug.WriteLine(m_lastError);  return false;  }

            Debug.WriteLine("Chapter " + chapter.Directory + " is done.");
            return true;
        }

        /// ---------------------------------------------------------------------------------------

        /// <summary>
        ///  將 Build() 所建立的所有網頁檔案都壓縮到  pathName 當中。
        /// </summary> 
        private Boolean BuildHtmlZip(String pathName)
        {
            ZipArchive archive;
            Debug.WriteLine("WebSiteBuilder.BuildHtmlZip(" + Path.GetFileName(pathName) + "):");

            try
            {   archive = ZipFile.Open(pathName, ZipArchiveMode.Create);   }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                Debug.WriteLine(m_lastError);
                return false;
            }

            Debug.WriteLine("Creating the Zip archive " + pathName + "...");

            /// 先把固定的檔案集合加入 zip archive 當中:
            String[] fileList = { "index.html", "toc.xhtml", "title.xhtml", "style.css", "viewer.css" };
            foreach (String fileName in fileList)
            {
                String srcPathName = Path.Combine(m_album.ContentFolder, fileName);
                Debug.WriteLine("Add " + srcPathName + " as " + fileName);
                try
                {   archive.CreateEntryFromFile(srcPathName, fileName, CompressionLevel.Optimal);  }
                catch (Exception ex)
                {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }
            }

            foreach (ChapterInfo chapter in m_album.Body.Chapters)
            {
                String fileName = chapter.Directory + ".xhtml";
                String srcPathName = Path.Combine(m_album.ContentFolder, fileName);
                Debug.WriteLine("Add " + srcPathName + " as " + fileName);

                try
                {   archive.CreateEntryFromFile(srcPathName, fileName, CompressionLevel.Optimal);  }
                catch (Exception ex)
                {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }
            }

            foreach (ChapterInfo chapter in m_album.Body.Chapters)
            {
                String chapEntryPath = chapter.Directory + "/";

                /// 加入每個 chapter 內的每個照片檔案及其 wj6.xhtml 網頁:
                foreach (Paragraph para in chapter.Body.Paragraphs)
                {
                    foreach (Photo photo in para.Photos)
                    {
                        String[] tokens = {  m_album.ContentFolder, chapter.Directory, photo.FileName  };
                        String srcPathName = Path.Combine(tokens);
                        Debug.WriteLine("Add " + srcPathName + " as " + chapEntryPath + photo.FileName);

                        try
                        {   archive.CreateEntryFromFile(srcPathName, chapEntryPath + photo.FileName, CompressionLevel.NoCompression);  }
                        catch (Exception ex)
                        {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }

                        String fileName = Path.GetFileNameWithoutExtension(photo.FileName) + ".wj6.xhtml";
                        tokens[2] = fileName;
                        srcPathName = Path.Combine(tokens);
                        Debug.WriteLine("Add " + srcPathName + " as " + chapEntryPath + fileName);

                        try
                        {   archive.CreateEntryFromFile(srcPathName, chapEntryPath + fileName, CompressionLevel.Optimal);  }
                        catch (Exception ex)
                        {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }
                    }
                }

                /// 加入每個 chapter 內的 thumbs 目錄下的縮圖檔案:
                String thumbEntryPath = chapEntryPath + "/thumbs/";

                foreach (Paragraph para in chapter.Body.Paragraphs)
                {
                    foreach (Photo photo in para.Photos)
                    {
                        String[] tokens = {  m_album.ContentFolder, chapter.Directory, "thumbs", photo.FileName  };
                        String srcPathName = Path.Combine(tokens);
                        Debug.WriteLine("Add " + srcPathName + " as " + thumbEntryPath + photo.FileName);

                        try
                        {   archive.CreateEntryFromFile(srcPathName, thumbEntryPath + photo.FileName, CompressionLevel.NoCompression);  }
                        catch (Exception ex)
                        {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }
                    }
                }
            }

            Debug.WriteLine("Done.");
            archive.Dispose();
            return true;
        }

        /// <summary>
        ///  這個函式在 Temp 目錄下產生同名的檔案，再把暫時檔案複製到 outPathName。
        /// </summary>
        public Boolean CreateZip(String outPathName)
        {
            String outFileName = Path.GetFileName(outPathName);

            /// 刪除既有的暫存檔案，如果存在的話...
            String tempPathName = Path.Combine(Path.GetTempPath(), outFileName);
            try { File.Delete(tempPathName); }
            catch (Exception ex) { m_lastError = ex.Message; return false; }

            /// 呼叫非同步方法 BuildHtmlZip():
            if (BuildHtmlZip(tempPathName) == false) { return false; }

            /// 檔案建立成功、把暫存目錄下的檔案移動到指定的路徑:
            try
            {
                if (File.Exists(outPathName) == true) {  File.Delete(outPathName);  }
                File.Move(tempPathName, outPathName);
            }
            catch (Exception ex)
            {   m_lastError = ex.Message;  return false;  }

            return true;
        }
    }
}
