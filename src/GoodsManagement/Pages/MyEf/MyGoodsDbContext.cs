using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

using MyUtility;

namespace MyEf
{
    public class MyGoodsDbContext : MyDbConfig
    {
        public override string DataBase { get; set; } = "MyGoodsData";

        public class PCInfo
        {
            [Key]
            [Required]
            public int Serial { get; set; }
            public string Name { get; set; }
            public string Thumbnail { get; set; }
            public string OS { get; set; }
            public string Shape { get; set; }
        }

        //上手くいかないので諦め (´・ω・｀)
        //public DbSet<PCInfo> MyPCInfo { get; set; }

        /// <summary>
        /// レコード一覧を取得
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="MenuList">テーブルの要素</param>
        /// <param name="Serial">シリアルNoを指定</param>
        /// <returns>レコード一覧</returns>
        public List<MyUI.MySummary> GetInfoList(string TableName, List<MyUI.MyMenu> MenuList, string Serial = "")
        {
            List<MyUI.MySummary> InfoList = new();
            string Filter = (string.IsNullOrEmpty(Serial)) ? "" : string.Format("serial = {0}", Serial);
            CDbTable DbWork = SelectRecods(TableName, "Name", Filter);
            int IndexSe = DbWork.IndexOf("Serial");
            int IndexTs = DbWork.IndexOf("Update");
            int IndexNa = DbWork.IndexOf("Name");
            int IndexTh = DbWork.IndexOf("Thumbnail");
            foreach (List<string> record in DbWork.Recods)
            {
                MyUI.MySummary info = new()
                {
                    Serial = record[IndexSe],
                    Update = DateTime.Parse(record[IndexTs]),
                    Name = record[IndexNa],
                    Thumbnail = record[IndexTh],
                    Explain = new(),
                };
                for (int i = 0; i < record.Count; i++)
                {
                    if (i.Equals(IndexSe)) { continue; }
                    if (i.Equals(IndexTs)) { continue; }
                    if (i.Equals(IndexNa)) { continue; }
                    if (i.Equals(IndexTh)) { continue; }
                    MyUI.MyExplain explain = new()
                    {
                        Name = MenuList.Find(item => item.Name.ToLower().Equals(DbWork.Fields[i])).Name,
                        Value = record[i],
                    };
                    info.Explain.Add(explain);
                }
                InfoList.Add(info);
            }
            return InfoList;
        }

        /// <summary>
        /// レコードを更新
        /// ※レコードを追加 or レコードを変更
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="Info">更新するレコード</param>
        /// <param name="OldUpdate">前回の更新日時</param>
        public void UpdateInfoList(string TableName, MyUI.MySummary Info, DateTime OldUpdate)
        {
            string Columns = "update , name , thumbnail";
            string Values = string.Format("'{0}' , '{1}' , '{2}'", Info.Update.ToString("yyyy-MM-dd HH:mm:ss"), Info.Name, Info.Thumbnail);
            foreach (MyUI.MyExplain explain in Info.Explain)
            {
                Columns += string.Format(" , {0}", explain.Name.ToLower());
                Values += string.Format(" , '{0}'", explain.Value);
            }
            if (string.IsNullOrEmpty(Info.Serial))
            {
                InsertRecod(TableName, Columns, Values);
            }
            else
            {
                UpdateRecod(TableName, Columns, Values, string.Format("serial = {0}", Info.Serial), OldUpdate.ToString("yyyy/MM/dd HH:mm:ss"));
            }
            SelectRecods(TableName);
        }
    }
}
