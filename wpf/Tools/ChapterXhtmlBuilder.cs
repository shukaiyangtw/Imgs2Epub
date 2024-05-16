/** @file ChapterXhtmlBuilder.cs
 *  @brief 輸出章節網頁.

 *  這個類別負責產生每一個章節的內文。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2019/2/23 */

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Imgs2Epub
{
    class ChapterXhtmlBuilder : BaseXhtmlBuilder
    {
        public ChapterXhtmlBuilder(ChapterInfo chapter)
        {
            Regex regex = new Regex(@"(\r\n|\r|\n)");
            m_title = chapter.Title;

            if (String.IsNullOrEmpty(m_title) == false)
            {
                m_body.Append("    <h1 class=\"title\">");
                m_body.Append(Escape(m_title));
                m_body.Append("</h1>\n");
            }

            foreach (Paragraph para in chapter.Body.Paragraphs)
            {
                m_body.Append("    <div class=\"paragraph\">\n");

                if (String.IsNullOrEmpty(para.Title) == false)
                {
                    m_body.Append("    <h2>");
                    m_body.Append(Escape(para.Title));
                    m_body.Append("</h2>\n");
                }

                if ((para.DateIsVisible == true) || (para.LocationIsVisible == true))
                {
                    m_body.Append("    <p class=\"datetime\">");
                    if (para.DateIsVisible == true)
                    {
                        m_body.Append(para.Date.ToString(AlbumInfo.DateFormat));
                        if (para.LocationIsVisible == true)
                        {
                            m_body.Append("  ");
                            m_body.Append(Properties.Resources.At);
                            m_body.Append("  ");
                        }
                    }

                    if (para.LocationIsVisible == true)
                    {
                        m_body.Append("<span class=\"locat\">");
                        m_body.Append(Escape(para.Location));
                        m_body.Append("</span>");
                    }

                    m_body.Append("</p>\n");
                }

                if (String.IsNullOrEmpty(para.Context) == false)
                {
                    String escStr = Escape(para.Context);
                    m_body.Append("    <p>");
                    m_body.Append(regex.Replace(escStr, "<br />"));
                    m_body.Append("</p>\n");
                }

                /// 產生每一張照片的 div:
                foreach (Photo photo in para.Photos)
                {
                    String href = chapter.Directory + "/" + Path.GetFileNameWithoutExtension(photo.FileName) + ".wj6.xhtml";

                    if (para.ThumbSize == 1)
                    {
                        if (photo.Orient == Photo.Orientation.Landscape)
                        {   m_body.Append("    <div class=\"landscape\">\n");  }
                        else
                        {   m_body.Append("    <div class=\"portrait\">\n");  }
                    }
                    else
                    {
                        if (photo.Orient == Photo.Orientation.Landscape)
                        {   m_body.Append("    <div class=\"landscape"); }
                        else
                        {   m_body.Append("    <div class=\"portrait"); }

                        m_body.Append(para.ThumbSize);
                        m_body.Append("x\">\n");
                    }

                    m_body.Append("        <a href=\"");
                    m_body.Append(href);
                    m_body.Append("\"><img src=\"");
                    m_body.Append(chapter.Directory);
                    m_body.Append("/thumbs/");
                    m_body.Append(photo.FileName);
                    m_body.Append("\" alt=\"\" /></a>\n");

                    if (String.IsNullOrEmpty(photo.Description) == false)
                    {
                        m_body.Append("        <span><a href=\"");
                        m_body.Append(href);
                        m_body.Append("\">");
                        m_body.Append(Escape(photo.Description));
                        m_body.Append("</a></span>\n");
                    }

                    m_body.Append("    </div>\n");
                }

                m_body.Append("</div>\n");
            }
        }
    }
}
