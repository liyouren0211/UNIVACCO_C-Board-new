using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardOPage
{
    public static class OCT_LogHelper
    {
        private static string ResultSavePath;    //儲存路徑
        private static readonly object LogSync = new object();

        public static void Init(string system_messege_save_path)
        {
            ResultSavePath = system_messege_save_path;
        }
        public static void WriteLog(LogLevel level, Page page, string message, string catcherror = "")
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

                // Keep all writers in this process serialized. FileShare.Read lets log viewers read.
                // Retry transient sharing violations from OTHER processes (or application instances).
                lock (LogSync)
                {
                    const int maxAttempts = 8;
                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                            using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
                            {
                                sw.WriteLine("{0} [{1}] {2}: {3}", DateTime.Now.ToLongTimeString(), emoji, pagename, message);
                                if (!string.IsNullOrWhiteSpace(catcherror))
                                    sw.WriteLine("　↳ 錯誤詳情：{0}", catcherror);
                            }
                            return;
                        }
                        catch (IOException ex)
                        {
                            // Windows error 32/33: sharing or locking violation.
                            int code = System.Runtime.InteropServices.Marshal.GetHRForException(ex) & 0xFFFF;
                            if ((code != 32 && code != 33) || attempt == maxAttempts)
                            {
                                Trace.WriteLine("Log write failed: " + ex);
                                return;
                            }
                            Thread.Sleep(25 * attempt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 日誌是輔助功能；寫入失敗不可中止檢測。
                Trace.WriteLine("Log write failed: " + ex);
            }
        }
    }
}
