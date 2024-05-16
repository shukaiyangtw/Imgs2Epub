/** @file BaseXhtmlBuilder.cs
 *  @brief 輸出網頁基本版型.

 *  為了提供所有電子書網頁符合 XHTML 規格的網頁檔案輸出功能，故以這個類別作為基礎類別。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2019/2/23 */

using System;
using System.IO;
using System.Text;

namespace Imgs2Epub
{
    class BaseXhtmlBuilder
    {
        /// <summary>
        ///  網頁標題、通常即相簿標題或章節標題。
        /// </summary>
        protected String m_title = "(Untitled)";

        /// <summary>
        ///  網頁樣式表。
        /// </summary>
        protected String m_cssFile = "style.css";

        /// <summary>
        ///  準備要輸出到 <body>...</body> 區段內的內容。
        /// </summary>
        protected StringBuilder m_body = new StringBuilder();

        /// <summary>
        ///  偵錯訊息。
        /// </summary>
        protected String m_lastError = String.Empty;
        public String LastError {  get {  return m_lastError;  }  }

        /// <summary>
        ///  將 m_body 的內容輸出為 pathName 所指定的 XHTML 檔案。
        /// </summary>
        public Boolean ExportToFile(String pathName)
        {
            StringBuilder sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.Append("<!DOCTYPE html>\n");
            sb.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" xml:lang=\"en\">\n");

            /// 建構 header 區段:
            sb.Append("<head>\n");
            sb.Append("    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\n");
            sb.Append("    <link rel=\"stylesheet\" href=\"");
            sb.Append(m_cssFile);
            sb.Append("\" type=\"text/css\" />\n");

            if (String.IsNullOrEmpty(m_title) == false)
            {
                sb.Append("    <title>");
                sb.Append(Escape(m_title));
                sb.Append("</title>\n");
            }

            sb.Append("</head>\n");

            /// 建構 body 區段:
            sb.Append("<body>\n");
            sb.Append(m_body.ToString());
            sb.Append("</body>\n</html>\n");

            /// 將 StringBuilder 的內容以 UTF-8 編碼寫入文字檔案:
            try
            {   File.WriteAllText(pathName, sb.ToString(), Encoding.UTF8);  }
            catch (Exception ex)
            {
                m_lastError = ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        ///  這五個字元應該要從 XML/XHTML 字串當中被去除。
        /// </summary>
        static public String Escape(String xmlStr)
        {
            String str1 = xmlStr.Replace("&", "&amp;");
            String str2 = str1.Replace("<", "&lt;");
            String str3 = str2.Replace(">", "&gt;");
            String str4 = str3.Replace("&#34;", "&quot;");
            return str4.Replace("'", "&apos;");
        }

        /// <summary>
        ///  這五個字元應該要從 XML/XHTML 字串當中被去除。
        /// </summary>
        static public String SafeFileName(String title)
        {
            String str1 = title.Replace(":", "-");
            String str2 = str1.Replace("<", "(");
            String str3 = str2.Replace(">", ")");
            String str4 = str3.Replace("/", "-");
            String str5 = str4.Replace("\\", "-");
            String str6 = str5.Replace("&#34;", "_");
            String str7 = str6.Replace("*", "_");
            return str7.Replace("?", "_");
        }
    }
}
