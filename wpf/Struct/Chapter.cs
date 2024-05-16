/** @file Chapter.cs
 *  @brief 章節內容

 *  這個程式用 ChapterInfo 和 Chapter 兩個類別來裝載章節資料，為了達到延遲載入、避免在程式一開始
 *  就載入所有的章節內容。在 ChapterInfo 類別當中只包括了章節標題、以及章節資料所在目錄的資訊，在
 *  Chapter 類別中包含 Paragraph 段落的集合、在 Paragraph 類別中包含 Photo 的集合。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2018/5/15 */

using System.Collections.ObjectModel;

namespace Imgs2Epub
{
    public sealed class Chapter
    {
        private ChapterInfo m_info;
        public ChapterInfo Header {  get {  return m_info;  }  }

        /// <summary>
        ///  每個章節的內容分為多個段落，每個段落包含一段說明文字以及數張圖片。
        /// </summary>
        public ObservableCollection<Paragraph> Paragraphs = new ObservableCollection<Paragraph>();

        public Chapter(ChapterInfo info)
        {
            m_info = info;
            info.Body = this;
        }
    }
}
