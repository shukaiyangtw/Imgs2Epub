/** @file AlbumEpubBuilder.cs
 *  @brief 相簿 EPUB 產生器

 *  這個類別會收集本相簿的檔案清單，並且產生必須的 content.opf 和 toc.ncx 檔案，然後一起壓縮成 zip 檔案。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2019/3/19 */

using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Windows;

namespace Imgs2Epub
{
    class AlbumEpubBuilder
    {
        /// <summary>
        ///  因為要 build 的相簿不一定是 app.CurAlbum，所以在建構式輸入物件。
        /// </summary>
        private AlbumInfo m_album;
        private String m_albumPath = String.Empty;

        // <summary>
        ///  偵錯訊息。
        /// </summary>
        private String m_lastError = String.Empty;
        public String LastError {  get {  return m_lastError;  }  }

        public AlbumEpubBuilder(AlbumInfo album)
        {
            m_album = album;
            m_albumPath = m_album.ContentFolder;
        }

        /// <summary>
        ///  重建 content.opf，因為搞不定 xmlns 所以用 StringBuilder 做。
        /// </summary>
        private Boolean BuildContentOpf()
        {
            Debug.WriteLine("Generating content.opf...");

            StringBuilder sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>\n");
            sb.Append("<package xmlns=\"http://www.idpf.org/2007/opf\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
            sb.Append(" xmlns:dcterms=\"http://purl.org/dc/terms/\" version=\"3.0\" xml:lang=\"en\"\n");
            sb.Append(" unique-identifier=\"EPB-UUID\">\n\n");

            #region <metadata>...</metadata>
            sb.Append("    <metadata>\n");

                sb.Append("        <dc:identifier id=\"EPB-UUID\">urn:uuid:");
                sb.Append(m_album.Identifier);
                sb.Append("</dc:identifier>\n");

                sb.Append("        <dc:title id=\"pub-title\">");
                sb.Append(BaseXhtmlBuilder.Escape(m_album.Title));
                sb.Append("</dc:title>\n");

                sb.Append("        <dc:date>");
                sb.Append(m_album.FirstDate.ToString("yyyy-MM-dd"));
                sb.Append("</dc:date>\n");

                sb.Append("        <meta property=\"dcterms:modified\">");
                sb.Append(DateTime.UtcNow.ToString("s"));
                sb.Append("Z</meta>\n");

                if (String.IsNullOrEmpty(m_album.CoverFile) == false)
                {   sb.Append("        <meta name=\"cover\" content=\"cover-image\" />\n");  }

                sb.Append("        <dc:creator id=\"author\">");
                sb.Append(BaseXhtmlBuilder.Escape(m_album.Author));
                sb.Append("</dc:creator>\n");

                sb.Append("        <dc:publisher>");
                sb.Append(BaseXhtmlBuilder.Escape(m_album.Author));
                sb.Append("</dc:publisher>\n");

            sb.Append("        <dc:language id=\"pub-language\">en</dc:language>\n");
            sb.Append("    </metadata>\n\n");
            #endregion

            #region <manifest>...</manifest>
            sb.Append("    <manifest>\n");

                sb.Append("        <item id=\"album_xml\" href=\"album.xml\" media-type=\"application/xml\" />\n");
                sb.Append("        <item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\" />\n");
                sb.Append("        <item id=\"stylesheet\" href=\"style.css\" media-type=\"text/css\" />\n");
                sb.Append("        <item id=\"viewer\" href=\"viewer.css\" media-type=\"text/css\" />\n");
                sb.Append("        <item id=\"frames\" href=\"index.html\" media-type=\"text/html\" />\n");
                sb.Append("        <item id=\"cover\" href=\"title.xhtml\" media-type=\"application/xhtml+xml\" />\n");

                if (String.IsNullOrEmpty(m_album.CoverFile) == false)
                {
                    sb.Append("        <item id=\"cover-image\" properties=\"cover-image\" href=\"");
                    sb.Append(m_album.CoverFile);
                    sb.Append("\" media-type=\"image/jpeg\" />\n");
                }

                sb.Append("        <item id=\"toc\" properties=\"nav\" href=\"toc.xhtml\" media-type=\"application/xhtml+xml\" />\n");

                foreach (ChapterInfo chapter in m_album.Body.Chapters)
                {
                    String prefix = chapter.Directory + "_";
                    String path = chapter.Directory + "/";
                    String thumbPath = path + "thumbs/";

                    sb.Append("        <item id=\"");
                    sb.Append(chapter.Directory);
                    sb.Append("\" href=\"");
                    sb.Append(chapter.Directory);
                    sb.Append(".xhtml\" media-type=\"application/xhtml+xml\" />\n");

                    sb.Append("        <item id=\"");
                    sb.Append(chapter.Directory);
                    sb.Append("_xml\" href=\"");
                    sb.Append(chapter.Directory);
                    sb.Append("/chapter.xml\" media-type=\"application/xml\" />\n");

                    foreach (Paragraph para in chapter.Body.Paragraphs)
                    {
                        foreach (Photo photo in para.Photos)
                        {
                            String mediaType = "image/jpeg";
                            String fileExt = Path.GetExtension(photo.FileName).ToLower();
                            if (fileExt.Equals(".png")) {  mediaType = "image/png"; }
                            String fileTitle = Path.GetFileNameWithoutExtension(photo.FileName);

                            /// item id="(chap)_(file title)" href="(chap)/(file name)" fallback="(chap)" media-type="image/jpeg" />
                            sb.Append("        <item id=\"");
                            sb.Append(prefix);
                            sb.Append(fileTitle);

                            sb.Append("\" href=\"");
                            sb.Append(path);
                            sb.Append(photo.FileName);

                            sb.Append("\" fallback=\"");
                            sb.Append(chapter.Directory);

                            sb.Append("\" media-type=\"");
                            sb.Append(mediaType);
                            sb.Append("\" />\n");

                            /// item id="(chap)_(file title)_xhtml" href="(chap)/(file title).wj6.xhtml" fallback="(chap)" media-type="application/xhtml+xml" />
                            sb.Append("        <item id=\"");
                            sb.Append(prefix);
                            sb.Append(fileTitle);

                            sb.Append("_xhtml\" href=\"");
                            sb.Append(path);
                            sb.Append(fileTitle);

                            sb.Append(".wj6.xhtml\" fallback=\"");
                            sb.Append(chapter.Directory);
                            sb.Append("\" media-type=\"application/xhtml+xml\" />\n");

                            /// item id="(chap)_(file title)t" href="(chap)/thumbs/(file name)" fallback="(chap)" media-type="image/jpeg" />
                            sb.Append("        <item id=\"");
                            sb.Append(prefix);
                            sb.Append(fileTitle);

                            sb.Append("t\" href=\"");
                            sb.Append(thumbPath);
                            sb.Append(photo.FileName);

                            sb.Append("\" fallback=\"");
                            sb.Append(chapter.Directory);

                            sb.Append("\" media-type=\"");
                            sb.Append(mediaType);
                            sb.Append("\" />\n");
                        }
                    }
                }

            sb.Append("    </manifest>\n\n");
            #endregion

            #region <spine toc="ncx">...</spine>
            sb.Append("    <spine toc=\"ncx\">\n");
            sb.Append("        <itemref idref=\"cover\" linear=\"yes\" />\n");
            sb.Append("        <itemref idref=\"toc\" linear=\"yes\" />\n");

                foreach (ChapterInfo chapter in m_album.Body.Chapters)
                {
                    String prefix = chapter.Directory + "_";
                    sb.Append("        <itemref idref=\"");
                    sb.Append(chapter.Directory);
                    sb.Append("\" linear=\"yes\" />\n");

                    foreach (Paragraph para in chapter.Body.Paragraphs)
                    { 
                        foreach (Photo photo in para.Photos)
                        {
                            String fileTitle = Path.GetFileNameWithoutExtension(photo.FileName);
                            sb.Append("        <itemref idref=\"");
                            sb.Append(prefix);
                            sb.Append(fileTitle);
                            sb.Append("_xhtml\" linear=\"no\" />\n");
                        }
                    }
                }

            sb.Append("    </spine>\n\n");
            sb.Append("</package>\n");
            #endregion

            /// 把 XML 字串寫入 content.opf:
            try
            {
                String pathName = Path.Combine(m_album.ContentFolder, "content.opf");
                Debug.WriteLine(pathName);
                File.WriteAllText(pathName, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        /// ---------------------------------------------------------------------------------------

        /// <summary>
        ///  重建 toc.ncx。
        /// </summary>
        private Boolean BuildTocNcx()
        {
            Debug.WriteLine("Generating toc.ncx...");

            StringBuilder sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>\n");
            sb.Append("<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\" xml:lang=\"en\">\n\n");

            #region <head>...</head>
            sb.Append("    <head>\n");

                sb.Append("        <meta name=\"dtb:uid\" content=\"urn:uuid:");
                sb.Append(m_album.Identifier);
                sb.Append("\" />\n");

                sb.Append("        <meta name=\"dtb:depth\" content=\"1\" />\n");
                sb.Append("        <meta name=\"dtb:totalPageCount\" content=\"0\" />\n");
                sb.Append("        <meta name=\"dtb:maxPageNumber\" content=\"0\" />\n");

            sb.Append("    </head>\n\n");
            #endregion

            #region <docTitle> and <docAuthor>
            sb.Append("    <docTitle><text>");
            sb.Append(BaseXhtmlBuilder.Escape(m_album.Title));
            sb.Append("</text></docTitle>\n");

            sb.Append("    <docAuthor><text>");
            sb.Append(BaseXhtmlBuilder.Escape(m_album.Author));
            sb.Append("</text></docAuthor>\n\n");
            #endregion

            #region <navMap>...</navMap>
            sb.Append("    <navMap>\n");
            sb.Append("        <navPoint id=\"cover\" playOrder=\"1\">\n");
            sb.Append("            <navLabel><text>");
            sb.Append(Properties.Resources.Preface);
            sb.Append("</text></navLabel>\n");
            sb.Append("            <content src=\"title.xhtml\" />\n");
            sb.Append("        </navPoint>\n");

            sb.Append("        <navPoint id=\"toc\" playOrder=\"2\">\n");
            sb.Append("            <navLabel><text>");
            sb.Append(Properties.Resources.TableOfContents);
            sb.Append("</text></navLabel>\n");
            sb.Append("            <content src=\"toc.xhtml\" />\n");
            sb.Append("        </navPoint>\n");


                int playOrder = 3;
                foreach (ChapterInfo chapter in m_album.Body.Chapters)
                {
                    sb.Append("        <navPoint id=\"");
                    sb.Append(chapter.Directory);
                    sb.Append("\" playOrder=\"");
                    sb.Append(playOrder);
                    sb.Append("\">\n");

                        sb.Append("            <navLabel><text>");
                        sb.Append(BaseXhtmlBuilder.Escape(chapter.Title));
                        sb.Append("</text></navLabel>\n");

                        sb.Append("            <content src=\"");
                        sb.Append(chapter.Directory);
                        sb.Append(".xhtml\" />\n");
                    sb.Append("        </navPoint>\n");

                    ++playOrder;
                }

            sb.Append("    </navMap>\n");
            sb.Append("</ncx>\n");
            #endregion

            /// 把 XML document 寫入 toc.ncx:
            try
            {
                String pathName = Path.Combine(m_album.ContentFolder, "toc.ncx");
                Debug.WriteLine(pathName);
                File.WriteAllText(pathName, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        /// ---------------------------------------------------------------------------------------
        private Boolean BuildEpubZip(String pathName)
        {
            ZipArchive archive;
            Debug.WriteLine("AlbumEpubBuilder.BuildEpubZip(" + m_album.Directory + ")...");

            try
            {   archive = ZipFile.Open(pathName, ZipArchiveMode.Create);  }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                Debug.WriteLine(m_lastError);
                return false;
            }

            Debug.WriteLine("Creating the Zip archive " + pathName + "...");

            /// 依照 EPUB 規定 mimetype 必須要是第一個檔案，且未壓縮:
            App app = Application.Current as App;
            String[] tokens = {  app.DataDir, m_album.Directory, "mimetype"  };
            String srcPathName = Path.Combine(tokens);

            try
            {   archive.CreateEntryFromFile(srcPathName, "mimetype", CompressionLevel.NoCompression);  }
            catch (Exception ex)
            {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }

            /// 接下來壓縮 META-INF/container.xml:
            tokens = new String[] {  app.DataDir, m_album.Directory, "META-INF", "container.xml"  };
            srcPathName = Path.Combine(tokens);

            try
            {   archive.CreateEntryFromFile(srcPathName, "META-INF/container.xml", CompressionLevel.Optimal);  }
            catch (Exception ex)
            {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }

            /// 先把 EPUB 目錄中的固定檔案集合加入 zip archive 當中:
            tokens = new String[] {  app.DataDir, m_album.Directory, "EPUB"  };
            String epubPath = Path.Combine(tokens);

            String[] fileList = new String[]
            {
                "content.opf", "toc.ncx",  "album.xml", "index.html",
                "toc.xhtml", "title.xhtml", "style.css", "viewer.css",
                m_album.CoverFile
            };

            foreach (String fileName in fileList)
            {
                if (String.IsNullOrEmpty(fileName) == false)
                { 
                    srcPathName = Path.Combine(epubPath, fileName);
                    Debug.WriteLine("Add " + srcPathName + " as EPUB/" + fileName);

                    try
                    {   archive.CreateEntryFromFile(srcPathName, "EPUB/" + fileName, CompressionLevel.Optimal);  }
                    catch (Exception ex)
                    {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }
                }
            }

            /// 壓縮每一章節的 .xhtml 檔案:
            foreach (ChapterInfo chapter in m_album.Body.Chapters)
            {
                String fileName = chapter.Directory + ".xhtml";
                srcPathName = Path.Combine(epubPath, fileName);
                Debug.WriteLine("Add " + srcPathName + " as EPUB/" + fileName);

                try
                {   archive.CreateEntryFromFile(srcPathName, "EPUB/" + fileName, CompressionLevel.Optimal);  }
                catch (Exception ex)
                {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }
            }

            foreach (ChapterInfo chapter in m_album.Body.Chapters)
            {
                String chapEntryPath = "EPUB/" + chapter.Directory + "/";
                String chapPath = Path.Combine(epubPath, chapter.Directory);

                /// 加入 chapter.xml:
                srcPathName = Path.Combine(chapPath, "chapter.xml");
                Debug.WriteLine("Add " + srcPathName + " as " + chapEntryPath + "chapter.xml");

                try
                {   archive.CreateEntryFromFile(srcPathName, chapEntryPath + "chapter.xml", CompressionLevel.Optimal);  }
                catch (Exception ex)
                {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }


                /// 加入每個 chapter 內的每個照片檔案及其 wj6.xhtml 網頁:
                foreach (Paragraph para in chapter.Body.Paragraphs)
                {
                    foreach (Photo photo in para.Photos)
                    {
                        srcPathName = Path.Combine(chapPath, photo.FileName);
                        Debug.WriteLine("Add " + srcPathName + " as " + chapEntryPath + photo.FileName);

                        try
                        {   archive.CreateEntryFromFile(srcPathName, chapEntryPath + photo.FileName, CompressionLevel.NoCompression);  }
                        catch (Exception ex)
                        {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }

                        String fileName = Path.GetFileNameWithoutExtension(photo.FileName) + ".wj6.xhtml";
                        srcPathName = Path.Combine(chapPath, fileName);
                        Debug.WriteLine("Add " + srcPathName + " as " + chapEntryPath + fileName);

                        try
                        {   archive.CreateEntryFromFile(srcPathName, chapEntryPath + fileName, CompressionLevel.Optimal);  }
                        catch (Exception ex)
                        {   m_lastError = ex.Message;  Debug.WriteLine(m_lastError);  return false;  }
                    }
                }

                /// 加入每個 chapter 內的 thumbs 目錄下的縮圖檔案:
                String thumbEntryPath = chapEntryPath + "thumbs/";
                String thumbPath = Path.Combine(chapPath, "thumbs");

                foreach (Paragraph para in chapter.Body.Paragraphs)
                {
                    foreach (Photo photo in para.Photos)
                    {
                        srcPathName = Path.Combine(thumbPath, photo.FileName);
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
        public Boolean CreateEpub(String outPathName)
        {
            String outFileName = Path.GetFileName(outPathName);
            Debug.WriteLine("AlbumEpubBuilder.CreateEpubAsync(" + outFileName + ")");

            /// 刪除既有的暫存檔案，如果存在的話...
            String tempPathName = Path.Combine(Path.GetTempPath(), outFileName);
            try {   File.Delete(tempPathName);  }
            catch (Exception ex) {  m_lastError = ex.Message;  return false;  }

            /// 檢查 content.opf 與 toc.ncx 相對於 album.xml 的檔案版本，是否需要重建?
            if (DiskHelper.IsNewerThan(m_album.ContentFolder, "content.opf", "album.xml") == false)
            {
                if (BuildContentOpf() == false) {  return false;  }
                if (BuildTocNcx() == false) {  return false;  }
            }

            /// 呼叫非同步方法 BuildEpubZip():
            if (BuildEpubZip(tempPathName) == false) {  return false;  }

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
