using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardOPage
{
    public static class OCT_LogHelper
    {
        private static string ResultSavePath;    //儲存路徑

        public static void Init(string system_messege_save_path)
        {
            ResultSavePath = system_messege_save_path;
        }
        public static void WriteLog(LogLevel level, Page page, string message,string catcherror = "")
        {
            try
            {
                string emoji = "";
                string pagename = "";
                switch (level)
                {
                    case LogLevel.Info:
                        emoji = "ℹ️";
                        break;
                    case LogLevel.Warning:
                        emoji = "⚠️";
                        break;
                    case LogLevel.Error:
                        emoji = "❌";
                        break;
                    default:
                        emoji = "";
                        break;
                }
                switch (page)
                {
                    case Page.A:
                        pagename = "A_Page";
                        break;
                    case Page.B:
                        pagename = "B_Page";
                        break;
                    case Page.O:
                        pagename = "O_Page";
                        break;
                    default:
                        pagename = "";
                        break;
                }
                // 使用 Path.Combine 結合路徑
                string dirName = Path.Combine(ResultSavePath, "Log");
                string fileName = Path.Combine(dirName, $"{DateTime.Now:yyyyMMdd}.txt");

                // 檢查並創建目錄（如果不存在）
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                // 使用 StreamWriter 寫入日誌，避免多餘的 File.Create 操作
                using (StreamWriter sw = new StreamWriter(fileName, true, Encoding.UTF8)) // UTF-8 編碼
                {
                    sw.WriteLine("{0} [{1}] {2}: {3}", DateTime.Now.ToLongTimeString(), emoji, pagename, message);
                    // 如果有錯誤補充訊息，就寫入一行
                    if (!string.IsNullOrWhiteSpace(catcherror))
                    {
                        sw.WriteLine("　↳ 錯誤詳情：{0}", catcherror);
                    }
                }
            }
            catch (Exception ex)
            {
                // 日誌寫入失敗時拋出清晰的錯誤
                throw new IOException($"寫入日誌失敗，錯誤訊息: {ex.Message}", ex);
            }
        }
    }
}
