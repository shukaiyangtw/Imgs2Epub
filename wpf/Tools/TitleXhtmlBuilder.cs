/** @file TitleXhtmlBuilde.cs
 *  @brief 輸出前言網頁.

 *  這個類別產生 title.xhtml 網頁。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2019/2/23 */

using System;
using System.Text.RegularExpressions;

namespace Imgs2Epub
{
    class TitleXhtmlBuilder : BaseXhtmlBuilder
    {
        public TitleXhtmlBuilder(AlbumInfo album)
        {
            m_title = album.Title;

            if (String.IsNullOrEmpty(m_title) == false)
            {
                m_body.Append("    <h1 class=\"title\" style=\"text-align: center\">");
                m_body.Append(Escape(m_title));
                m_body.Append("</h1>\n");
            }

            if (String.IsNullOrEmpty(album.Author) == false)
            {
                m_body.Append("    <p class=\"author\" style=\"text-align: center\">");
                m_body.Append(Escape(album.Author));
                m_body.Append("</p>\n");
            }

            m_body.Append("    <p class=\"datetime\" style=\"text-align: center\">");
            m_body.Append(album.DateStr);
            if (String.IsNullOrEmpty(album.Location) == false)
            {
                m_body.Append("<br /><span class=\"locat\">\n");
                m_body.Append(Escape(album.Location));
                m_body.Append("</span></p>\n");
            }
            else
            {   m_body.Append("</p>\n");  }

            if (String.IsNullOrEmpty(album.Body.Text) == false)
            {
                Regex regex = new Regex(@"(\r\n|\r|\n)");
                String escStr = Escape(album.Body.Text);
                m_body.Append("    <p class=\"introduction\">");
                m_body.Append(regex.Replace(escStr, "<br />"));
                m_body.Append("</p>\n");
            }
        }
    }
}
