/// -----------------------------------------------------------------------------------------------
/// <summary>
///     主要導覽視窗
/// </summary>
/// <remarks>
///     這個衍生自 NavigationWindow 用來導覽 AlbumListPage, MainPage, CoverPage, 以及 ChapterPage。
/// </remarks>
/// <history>
///     2023/8/18 by Shu-Kai Yang (skyang@nycu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Navigation;
using Imgs2Epub.Properties;

namespace Imgs2Epub
{
    public partial class MainWindow : NavigationWindow
    {      
        public MainWindow()
        {
            InitializeComponent();
            Title = Properties.Resources.AppTitle;
            this.Navigating += OnNavigating;

            /// 載入先前儲存的視窗尺寸與位置。
            if (Settings.Default.WindowPos != null)
            {
                this.Left = Settings.Default.WindowPos.X;
                this.Top = Settings.Default.WindowPos.Y;
            }

            if (Settings.Default.WindowSize != null)
            {
                this.Width = Settings.Default.WindowSize.Width;
                this.Height = Settings.Default.WindowSize.Height;
            }

            if (Settings.Default.ViewerPos != null)
            {
                PhotoDialog.ViewerPos.X = Settings.Default.ViewerPos.X;
                PhotoDialog.ViewerPos.Y = Settings.Default.ViewerPos.Y;
            }

            if (Settings.Default.ViewerSize != null)
            {
                PhotoDialog.ViewerSize.Width = Settings.Default.ViewerSize.Width;
                PhotoDialog.ViewerSize.Height = Settings.Default.ViewerSize.Height;
            }
        }

        /// <summary>
        ///  讓 F5 失去作用，否則 NavigationWindow 會把 Page 銷毀再重建。
        /// </summary>
        void OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Refresh)
            {   e.Cancel = true;  }
        }

        /// <summary>
        ///  在視窗要關閉之前，儲存目前編輯中的相簿專案，並且記錄視窗尺寸與位置。
        /// </summary>
        private void NavigationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /// 因為可能是使用者在任何頁面按下 X 按鈕而關閉的，必須儲存頁面資料:
            if (NavigationService.Content.GetType() == typeof(MainPage))
            {
                var page = NavigationService.Content as MainPage;
                page.SaveChanges();
            }
            else if (NavigationService.Content.GetType() == typeof(ChapterPage))
            {
                var page = NavigationService.Content as ChapterPage;
                page.SaveChanges();
            }

            /// 程式即將暫停或停止，把相簿清單儲存回 albums.xml:
            App app = Application.Current as App;
            if ((app.CurChap != null)  && (app.CurChap.IsModified))  {  app.CurChap.SaveXml();  }
            if ((app.CurAlbum != null) && (app.CurAlbum.IsModified)) {  app.CurAlbum.SaveXml();  }
            if (AlbumInfo.IsCollectionModified == true) {  app.SaveAlbumsXml();  }

            /// 在視窗要關閉之前，記錄視窗尺寸與位置：
            if (this.WindowState == WindowState.Normal)
            {
                Settings.Default.WindowPos = new System.Drawing.Point((int)this.Left, (int)this.Top);
                Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
            }
            else
            {
                Settings.Default.WindowPos = new System.Drawing.Point((int)RestoreBounds.Left, (int)RestoreBounds.Top);
                Settings.Default.WindowSize = new System.Drawing.Size((int)RestoreBounds.Size.Width, (int)RestoreBounds.Size.Height);
            }

            Settings.Default.ViewerPos = PhotoDialog.ViewerPos;
            Settings.Default.ViewerSize = PhotoDialog.ViewerSize;
            Settings.Default.Save();
        }
    }
}
