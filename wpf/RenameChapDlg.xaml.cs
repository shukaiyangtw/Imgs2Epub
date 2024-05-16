/** @file RenameChapDlg.xaml.cs
 *  @brief 修改篇章子目錄與標題

 *  在這個對話框提供使用者輸入篇章的子目錄名稱與標題。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/8/29 */

using System;
using System.Windows;

namespace Imgs2Epub
{
    public partial class RenameChapDlg : Window
    {
        /// <summary>
        ///  頁面上的資料成員。
        /// </summary>
        public String SubDirectory = "ch001";
        public String ChapTitle = String.Empty;

        public RenameChapDlg()
        {   InitializeComponent();  }

        /// <summary>
        ///  載入資料成員到頁面上的控制項。
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ChapDirTextBox.Text = SubDirectory;
            ChapTitleTextBox.Text = ChapTitle;
            ChapTitleTextBox.Focus();
        }

        /// <summary>
        ///  把頁面上的資料抄錄到資料成員。
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ChapDirTextBox.Text) == true)
            {   MessageLabel.Text = Properties.Resources.NonEmptyDirNameMsg;  return;  }

            /// 檢查 SubDirectory 是否為安全的檔名:
            if (ChapDirTextBox.Text.Equals(SubDirectory) == false)
            {   SubDirectory = BaseXhtmlBuilder.SafeFileName(ChapDirTextBox.Text);  }

            if (String.IsNullOrEmpty(ChapTitleTextBox.Text) == true)
            {   ChapTitle = Properties.Resources.UntitledChapTitle;     }
            else {  ChapTitle = ChapTitleTextBox.Text;  }

            DialogResult = true;
        }

        /// <summary>
        ///  不做任何事結束。
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {   DialogResult = false;  }
    }
}
