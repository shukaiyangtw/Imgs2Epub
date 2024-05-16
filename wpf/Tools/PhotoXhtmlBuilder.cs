/** @file PhotoXhtmlBuilder.cs
 *  @brief 輸出單張照片網頁.

 *  這個類別針對指定的 Photo 物件產生以全畫面觀察單張照片的網頁。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2019/2/23 */

using System;

namespace Imgs2Epub
{
    class PhotoXhtmlBuilder : BaseXhtmlBuilder
    {
        public PhotoXhtmlBuilder(Photo photo)
        {
            m_cssFile = "../viewer.css";

            if (String.IsNullOrEmpty(photo.Description) == false)
            {   m_title = photo.Description;  }
            else {  m_title = photo.FileName;  }

            m_body.Append("<div class=\"fullscreenimage\">\n");
            m_body.Append("    <img src=\"");
            m_body.Append(photo.FileName);
            m_body.Append("\" alt=\"\" />\n");

            if (String.IsNullOrEmpty(photo.Description) == false)
            {
                m_body.Append("    <div class=\"toolbar\">\n");
                m_body.Append("        <span>");
                m_body.Append(Escape(photo.Description));
                m_body.Append("</span>\n");
                m_body.Append("    </div>\n");
            }

            m_body.Append("</div>\n");
        }
    }
}

