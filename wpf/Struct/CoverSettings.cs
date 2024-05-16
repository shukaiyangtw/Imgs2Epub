/** @file CoverSettings.cs
 *  @brief 封面影像編輯設定

 *  這個類別儲存了關於封面影像的編輯資訊。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/9/20 */

using System;
using System.Xml;
using System.Drawing;

namespace Imgs2Epub
{
    public sealed class CoverSettings
    {
        /// 編輯相簿封面的資訊:
        public String FontFamily = "Ariel";
        public int FontStyle = 0;
        public int TextSize = 48;

        public Color TextColor = Color.White;
        public int BgIndex = 0;

        /// 選為封面照片的圖檔相對路徑:
        public String CoverRawFile = String.Empty;

        public CoverSettings()
        {

        }

        /* 編輯相簿封面用的資訊
        <cover_editor>
            <font>Arial</font>
            <style>0</style>
            <textsize>48</textsize>
            <textcolor>#FFFFFF</textcolor>
            <bgindex>0</bgindex>
            <raw>相簿原始圖檔</raw>
        </cover_editor> */
        public void ReadXmlElement(XmlElement element)
        {
            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    XmlElement ele = child as XmlElement;
                    if (ele.Name.Equals("font"))
                    {   FontFamily = ele.InnerText;  }
                    else if (ele.Name.Equals("style"))
                    {   FontStyle = Int32.Parse(ele.InnerText);  }
                    else if (ele.Name.Equals("textsize"))
                    {   TextSize = Int32.Parse(ele.InnerText);  }
                    else if (ele.Name.Equals("textcolor"))
                    {   TextColor = ColorTranslator.FromHtml(ele.InnerText);  }
                    else if (ele.Name.Equals("bgindex"))
                    {   BgIndex = Int32.Parse(ele.InnerText);  }
                    else if (ele.Name.Equals("raw"))
                    {   CoverRawFile = ele.InnerText;  }
                }
            }
        }

        public XmlElement CreateXmlElement(XmlDocument doc)
        {
            XmlElement element = doc.CreateElement("cover_editor");
            XmlElement child = doc.CreateElement("textsize");
            XmlText text = doc.CreateTextNode(TextSize.ToString());
            child.AppendChild(text);
            element.AppendChild(child);

            child = doc.CreateElement("textcolor");
            String str = ColorTranslator.ToHtml(TextColor);
            text = doc.CreateTextNode(str);
            child.AppendChild(text);
            element.AppendChild(child);

            child = doc.CreateElement("bgindex");
            text = doc.CreateTextNode(BgIndex.ToString());
            child.AppendChild(text);
            element.AppendChild(child);

            if (String.IsNullOrEmpty(FontFamily) == false)
            {
                child = doc.CreateElement("font");
                text = doc.CreateTextNode(FontFamily);
                child.AppendChild(text);
                element.AppendChild(child);
            }

            if (FontStyle != 0)
            {
                child = doc.CreateElement("style");
                text = doc.CreateTextNode(FontStyle.ToString());
                child.AppendChild(text);
                element.AppendChild(child);
            }

            if (String.IsNullOrEmpty(CoverRawFile) == false)
            {
                child = doc.CreateElement("raw");
                text = doc.CreateTextNode(CoverRawFile);
                child.AppendChild(text);
                element.AppendChild(child);
            }

            return element;
        }
    }
}
