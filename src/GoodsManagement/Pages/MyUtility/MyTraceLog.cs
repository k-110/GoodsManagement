using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyUtility
{
    public static class MyUtilityLog
    {
        private static readonly MyTraceLog UtilityTraceLog = new();

        /// <summary>
        ///  ログ処理
        /// </summary>
        /// <param name="LogText">ログする文字列</param>
        public static void Write(string LogText)
        {
            UtilityTraceLog.Write(LogText);
        }

        /// <summary>
        /// ログ処理＆例外発生
        /// </summary>
        /// <param name="ExceptionText">例外のメッセージ</param>
        public static void ThrowException(string ExceptionText)
        {
            UtilityTraceLog.Write(ExceptionText);
            throw new Exception(ExceptionText);
        }
    }

    public class MyTraceLog
    {
        private readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"log");
        private readonly string FileName = "yyyyMMdd_TraceLog.log";
        private readonly object LogLock = new();

        /// <summary>
        /// ログ追記
        /// </summary>
        /// <param name="LogText">ログする文字列</param>
        public void Write(string LogText)
        {
            lock (LogLock)
            {
                try
                {
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    DateTime date = DateTime.Now;
                    string LogFilePath = System.IO.Path.Combine(LogDirectory, FileName.Replace("yyyyMMdd", date.ToString("yyyyMMdd")));
                    using (StreamWriter sw = new(LogFilePath, true, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine(date.ToString("yyyy/MM/dd HH:mm:ss\t") + LogText);
                        sw.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
        }
    }
}
