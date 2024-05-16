/** @file TocXhtmlBuilder.cs
 *  @brief 輸出章節目錄.

 *  這個類別產生 toc.xhtml 網頁。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2019/2/23 */

namespace Imgs2Epub
{
    class TocXhtmlBuilder : BaseXhtmlBuilder
    {
        public TocXhtmlBuilder(AlbumInfo album)
        {
            m_title = Properties.Resources.TableOfContents;

            m_body.Append("    <h1>");
            m_body.Append(m_title);
            m_body.Append("</h1>\n");

            m_body.Append("    <nav epub:type=\"toc\" id=\"toc\">\n");
            m_body.Append("    <ol style=\"list-style-type: none\">\n");
            m_body.Append("        <li class=\"toc\"><a href=\"title.xhtml\" target=\"viewer\">");
            m_body.Append(Properties.Resources.Preface);
            m_body.Append("</a></li>\n");

            foreach (ChapterInfo chapter in album.Body.Chapters)
            {
                m_body.Append("        <li class=\"toc\"><a href=\"");
                m_body.Append(chapter.Directory);
                m_body.Append(".xhtml\" target=\"viewer\">");
                m_body.Append(Escape(chapter.Title));
                m_body.Append("</a></li>\n");
            }

            m_body.Append("    </ol>\n");
            m_body.Append("    </nav>\n");
        }
    }
}
