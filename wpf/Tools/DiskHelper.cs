/** @file DiskHelper.cs
 *  @brief 磁碟檔案或目錄工具.

 *  這裡實作一些常用的磁碟檔案或子目錄的操作。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/8/14 */

using System;
using System.IO;
using System.Text;

namespace Imgs2Epub
{
    public sealed class DiskHelper
    {
        /* 這裡將 0..35 的數字對應到字元，用來由日期等數字產生唯一的檔名。 */
        static readonly Char[] intToChar =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f',
            'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z'
        };

        /* 這個函式利用 intToChar[] 和 DateTime.Now 產生一個暫時唯一的字串，可做為檔案或目錄名稱。 */
        public static String GenerateNameByDate()
        {
            StringBuilder sb = new StringBuilder();
            DateTime dt = DateTime.Now;
            int yearHigh = dt.Year / intToChar.Length;
            int yearLow = dt.Year % intToChar.Length;
            sb.Append(intToChar[yearHigh % intToChar.Length]);
            sb.Append(intToChar[yearLow]);

            sb.Append(intToChar[dt.Month]);
            sb.Append(intToChar[dt.Day]);

            sb.Append(intToChar[dt.Hour]);
            int seconds = dt.Minute * 60 + dt.Second;
            int i = seconds / 100;
            int j = seconds % 100;

            sb.Append(intToChar[i]);
            sb.Append(j.ToString("D2"));
            return sb.ToString();
        }

        /// <summary>
        ///  檢查目錄 folder 中的磁碟檔案 fileName 是否存在，而且檔案日期確實比 oldFile 新。
        /// </summary>
        static public Boolean IsNewerThan(String folder, String fileName, String oldFileName)
        {
            String pathName1 = Path.Combine(folder, fileName);
            if (File.Exists(pathName1) == false) {  return false;  }
            String pathName2 = Path.Combine(folder, oldFileName);
            return IsFileNewerThan(pathName1, pathName2);
        }

        /// <summary>
        ///  檢查檔案是否需要重新產製，當 pathName1 的日期較新或 pathName2 根本不存在的時候，傳回 true。
        /// </summary>
        static public Boolean IsFileNewerThan(String pathName1, String pathName2)
        {
            if (File.Exists(pathName1) == false) {  return false;  }
            if (File.Exists(pathName2) == false) {  return true;  }

            DateTime dt1 = File.GetLastWriteTime(pathName1);

         /* 除非徹底地刪除舊檔案再重建，否則檔案重新覆寫的時候，creation time 仍然不會變，因此檢查檔案版本應
          * 該以 modified time (last-write time) 為準，然而作業系統基於自己的最佳化原則，不一定會在每一次
          * 寫入檔案的時候立即寫入正確的 modified time，此外，複製檔案的時候，產生的新檔案之 creation time
          * 是複製檔案的時間，可是 modified time 卻仍然是舊檔案的最後修改時間，因此會產生 modified time 比
          * creation time 還早的奇怪現象，因此要盡可能正確地比較檔案版本，要取兩者的最新值。 */
            DateTime dt = File.GetCreationTime(pathName1);
            if (DateTime.Compare(dt, dt1) > 0) {  dt1 = dt;  }

            DateTime dt2 = File.GetLastWriteTime(pathName2);
            dt = File.GetCreationTime(pathName2);
            if (DateTime.Compare(dt, dt2) > 0) {  dt2 = dt;  }

            if (DateTime.Compare(dt1, dt2) > 0) {  return true; }
            return false;
        }
    }

}
