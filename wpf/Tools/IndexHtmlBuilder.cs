/** @file IndexHtmlBuilder.cs
 *  @brief 輸出相簿網站首頁.

 *  這個類別單純地輸出一個包含 frameset 的 index.html 檔案，標題是相簿標題，左邊是 toc.xhtml 右邊是 title.xhtml。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2019/2/23 */

using System;
using System.IO;
using System.Text;

namespace Imgs2Epub
{
    class IndexHtmlBuilder
    {
        /// <summary>
        ///  建構式所產生的 HTML 內容。
        /// </summary>
        protected StringBuilder m_html = new StringBuilder();

        /// <summary>
        ///  偵錯訊息。
        /// </summary>
        protected String m_lastError = String.Empty;
        public String LastError {  get {  return m_lastError;  }  }

        public IndexHtmlBuilder(AlbumInfo album)
        {
            m_html.Append("<!DOCTYPE html>\n");
            m_html.Append("<html>\n<head>\n");

            m_html.Append("    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\n");
            if (String.IsNullOrEmpty(album.Title) == false)
            {
                m_html.Append("    <title>");
                m_html.Append(BaseXhtmlBuilder.Escape(album.Title));
                m_html.Append("</title>\n");
            }

            m_html.Append("</head>\n<frameset cols=\"20%,*\">\n");
            m_html.Append("    <frame name=\"toc\" src=\"toc.xhtml\" />\n");
            m_html.Append("    <frame name=\"viewer\" src=\"title.xhtml\" />\n");
            m_html.Append("</frameset>\n</html>");

        }

        /// <summary>
        ///  將 m_body 的內容輸以 UTF-8 編碼寫入指定的 XHTML 檔案。
        /// </summary>
        public Boolean ExportToFile(String pathName)
        {
            try
            {   File.WriteAllText(pathName, m_html.ToString(), Encoding.UTF8);  }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                return false;
            }

            return true;
        }
    }
}
