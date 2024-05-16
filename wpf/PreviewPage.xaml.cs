/** @file PreviewPage.xaml.cs
 *  @brief 預覽生成的相簿網頁

 *  這個頁面單純地內嵌一個 WebBrowser 網頁瀏覽器去預覽目前已經生成的頁面。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/9/20 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Imgs2Epub
{
    public partial class PreviewPage : Page
    {
        public class UrlItem
        {
            public String Title { get; set; }
            public String Url { get; set; }

            public UrlItem(string title, string url)
            {
                Title = title;
                Url = url;
            }
        }

        /// 用來 ListView 綁定的項目。
        private List<UrlItem> UrlItems = new List<UrlItem>();

        /// <summary>
        ///  在建構式中指定起始的網頁。
        /// </summary>
        private String m_startPage = "title.xhtml";

        public PreviewPage(string startPage)
        {
            m_startPage = startPage;
            InitializeComponent();

            var app = Application.Current as App;
            UrlItems.Add(new UrlItem(Properties.Resources.Preface, "title.xhtml"));

            foreach (ChapterInfo chapter in app.CurAlbum.Body.Chapters)
            {   UrlItems.Add(new UrlItem(chapter.Title, chapter.Directory + ".xhtml"));  }

            TocView.ItemsSource = UrlItems;
        }

        /// <summary>
        ///  瀏覽起始的網頁。
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            String filePath = Path.Combine(app.CurAlbum.ContentFolder, m_startPage);
            String fileUrl = "file:///" + filePath.Replace('\\', '/');
            XhtmlView.Navigate(fileUrl);
        }

        /// <summary>
        ///  結束預覽。
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {   this.NavigationService.GoBack();  }
        }

        /// <summary>
        ///  回到上一頁。
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (XhtmlView.CanGoBack)
            {   XhtmlView.GoBack();  }
        }

        /// <summary>
        ///  前往指定的頁面。
        /// </summary>
        private void TocView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TocView.SelectedItem != null)
            {
                var app = Application.Current as App;
                UrlItem item = TocView.SelectedItem as UrlItem;
                String filePath = Path.Combine(app.CurAlbum.ContentFolder, item.Url);
                String fileUrl = "file:///" + filePath.Replace('\\', '/');
                XhtmlView.Navigate(fileUrl);
            }
        }

        /// <summary>
        ///  返回預覽的第一頁。
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            String filePath = Path.Combine(app.CurAlbum.ContentFolder, m_startPage);
            String fileUrl = "file:///" + filePath.Replace('\\', '/');
            XhtmlView.Navigate(fileUrl);
        }
    }
}
