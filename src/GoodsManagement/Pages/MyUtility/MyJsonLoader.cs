using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Json.Serialization;

namespace MyUtility
{
    public class MyJsonLoader<T>
    {
        public string FilePath { get; private set; }
        public T Data { get; set; }
        private readonly object JsonLock = new();
        private DateTime JsonDate;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="JsonFilePath">ファイルパス</param>
        public MyJsonLoader(string JsonFilePath)
        {
            FilePath = JsonFilePath;
            JsonDate = (File.Exists(FilePath)) ? File.GetLastWriteTime(FilePath) : DateTime.Now;
        }

        /// <summary>
        /// 日時を示す文字列を取得
        /// </summary>
        /// <returns>日時を示す文字列</returns>
        public string GetDateText()
        {
            return JsonDate.ToString("yyyy/MM/dd HH:mm:ss.ffff");
        }

        /// <summary>
        /// データをロードする処理
        /// </summary>
        /// <returns>処理の実行結果</returns>
        public bool Load()
        {
            lock (JsonLock)
            {
                if (File.Exists(FilePath))
                {
                    try
                    {
                        string JsonText = File.ReadAllText(FilePath, System.Text.Encoding.UTF8);
                        Data = JsonSerializer.Deserialize<T>(JsonText);
                        JsonDate = File.GetLastWriteTime(FilePath);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MyUtilityLog.Write(ex.ToString());
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// データをセーブする処理
        /// </summary>
        /// <param name="DateText">競合の有無確認用の日時を示す文字列</param>
        /// <returns>処理の実行結果</returns>
        public bool Save(string DateText)
        {
            lock (JsonLock)
            {
                try
                {
                    JsonSerializerOptions options = new(){
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        WriteIndented = true
                    };
                    bool IsFileExists = File.Exists(FilePath);
                    string JsonText = JsonSerializer.Serialize(Data, options);
                    using (FileStream fs = File.OpenWrite(FilePath))
                    {
                        bool IsNoConflict = File.GetLastWriteTime(FilePath).ToString("yyyy/MM/dd HH:mm:ss.ffff").Equals(DateText); ;
                        if (IsNoConflict || !IsFileExists)
                        {
                            byte[] WriteData = System.Text.Encoding.UTF8.GetBytes(JsonText);
                            fs.Write(WriteData, 0, WriteData.Length);
                            fs.SetLength(WriteData.Length);
                            IsNoConflict = true;
                        }
                        fs.Close();
                        JsonDate = File.GetLastWriteTime(FilePath);
                        return IsNoConflict;
                    }
                }
                catch (Exception ex)
                {
                    MyUtilityLog.Write(ex.ToString());
                }
            }
            return false;
        }

    }
}
