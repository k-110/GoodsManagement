using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;

using MyUtility;

namespace MyEf
{
    public class MyDbConfig : DbContext
    {
        /// <summary>
        ///  データベースのテーブル型フォーマット
        /// </summary>
        protected class CDbTable
        {
            public bool Enable;
            public List<string> Fields;
            public List<List<string>> Recods;

            public CDbTable()
            {
                Enable = false;
                Fields = new();
                Recods = new();
            }

            public int IndexOf(string FieldName)
            {
                //CreateTableで作成するとアルファベットが小文字の要素名になる
                return Fields.IndexOf(FieldName.ToLower());
            }
        }

        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public virtual string DataBase { get; set; } = "";
        public virtual string Username { get; set; } = "postgres";
        public virtual string Password { get; set; } = "postgres";

        private const int MaxRetry = 3;
        private const int RetryInterval = 1000;

        private readonly object LockMyMyDbConfig = new();

        protected override void OnConfiguring(DbContextOptionsBuilder OptionsBuilder)
        {
            OptionsBuilder.UseNpgsql(GetConnectionString());
        }

        /// <summary>
        /// 接続設定を取得
        /// </summary>
        /// <returns>接続設定</returns>
        private string GetConnectionStringForDoSql()
        {
            string ConnectionString = string.Format("Host={0};Port={1};Username={2};Password={3}",
                Host, Port, Username, Password
            );
            return ConnectionString;
        }

        /// <summary>
        /// 接続設定を取得
        /// </summary>
        /// <returns>接続設定</returns>
        private string GetConnectionString()
        {
            string ConnectionString = GetConnectionStringForDoSql();
            if (!string.IsNullOrEmpty(DataBase))
            {
                //CreateDbで作成するとアルファベットが小文字のDB名になる
                ConnectionString += string.Format(";Database={0}", DataBase.ToLower());
            }
            return ConnectionString;
        }

        /// <summary>
        /// SQLを実行
        /// </summary>
        /// <param name="SqlText">SQL</param>
        /// <param name="DbSelect">指定のDBに接続する場合にtrue</param>
        /// <param name="SqlUpdateText">前回の更新日時を取得するSQL</param>
        /// <param name="OldUpdateText">前回の更新日時</param>
        /// <returns>実行結果</returns>
        private CDbTable DoSql(bool DbSelect, string SqlText, string SqlUpdateText = null, string OldUpdateText = null)
        {
            CDbTable DbWork = new();
            lock (LockMyMyDbConfig)
            {
                MyUtilityLog.Write(string.Format("{0}:{1}", MethodBase.GetCurrentMethod().Name, SqlText));
                for (int cnt = 0; (cnt < MaxRetry) && (!DbWork.Enable); cnt++)
                {
                    try
                    {
                        using (NpgsqlConnection MyConnection = new((DbSelect) ? GetConnectionString() : GetConnectionStringForDoSql()))
                        using (NpgsqlCommand MyCommand = new(SqlText, MyConnection))
                        {
                            MyConnection.Open();
                            if (!string.IsNullOrEmpty(OldUpdateText))
                            {
                                //更新日時が変わっていないことを確認
                                using (NpgsqlCommand MyUpdate = new(SqlUpdateText, MyConnection))
                                using (NpgsqlDataReader ExReader = MyUpdate.ExecuteReader())
                                {
                                    string UpdateText = "";
                                    while (ExReader.Read())
                                    {
                                        UpdateText = ExReader.GetValue(0).ToString();
                                    }
                                    ExReader.Close();
                                    if (!DateTime.Parse(UpdateText).Equals(DateTime.Parse(OldUpdateText)))
                                    {
                                        throw new Exception(string.Format("Conflict:{0}", SqlUpdateText));
                                    }
                                }
                            }
                            using (NpgsqlDataReader ExReader = MyCommand.ExecuteReader())
                            {
                                //フィールドの作成
                                for (int i = 0; i < ExReader.FieldCount; i++)
                                {
                                    DbWork.Fields.Add(ExReader.GetName(i)); ;
                                }

                                //レコードの作成
                                while (ExReader.Read())
                                {
                                    List<string> TempWork = new();
                                    for (int i = 0; i < DbWork.Fields.Count; i++)
                                    {
                                        TempWork.Add(ExReader.GetValue(i).ToString());
                                    }
                                    DbWork.Recods.Add(TempWork);
                                }
                                DbWork.Enable = true;
                                ExReader.Close();
                            }
                            MyConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MyUtilityLog.Write(ex.ToString());
                        System.Threading.Thread.Sleep(RetryInterval);
                    }
                }
            }
            return DbWork;
        }

        /// <summary>
        /// SQL失敗時に例外を発生させる
        /// </summary>
        /// <param name="MethodName"></param>
        private void ThrowException([CallerMemberName] string MethodName = "")
        {
            string ExText = string.Format("{0}.{1} NG:Sql command failed.", this.GetType().Name, MethodName);
            MyUtilityLog.Write(ExText);
            throw new Exception(ExText);
        }

        /// <summary>
        /// SQL実行：データベース一覧を取得
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <returns>データベース一覧</returns>
        public List<string> GetDbList()
        {
            //所有者はdatdbaではなくpg_authid.rolname as dbrollnameとjoin pg_authid on pg_authid.oid = pg_database.datdbaでロール名を取得している
            string SqlText = string.Format("SELECT datname, pg_authid.rolname as dbrollname, pg_encoding_to_char(encoding), datcollate, datctype FROM pg_database join pg_authid on pg_authid.oid = pg_database.datdba;");
            CDbTable DbWork = DoSql(false, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            int index = DbWork.IndexOf("datname");
            List<string> DbList = new();
            if (0 <= index)
            {
                foreach (List<string> recode in DbWork.Recods)
                {
                    DbList.Add(recode[index]);
                }
            }
            return DbList;
        }

        /// <summary>
        /// SQL実行：データベースが存在するか？
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="DbName">データベース名</param>
        /// <returns>有無</returns>
        private bool IsDbExists(string DbName)
        {
            //CreateDbで作成するとアルファベットが小文字のDB名になる
            return (0 <= GetDbList().IndexOf(DbName.ToLower()));
        }

        /// <summary>
        /// SQL実行：データベースを作成する
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="DbName">データベース名</param>
        public void CreateDb(string DbName)
        {
            if (IsDbExists(DbName))
            {
                //同名のDBが存在する
                return;
            }
            string SqlText = string.Format("CREATE DATABASE {0};", DbName);
            CDbTable DbWork = DoSql(false, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            else if (!IsDbExists(DbName))
            {
                //同名のDBが存在しない場合は作成失敗
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：データベースを削除する
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="DbName">データベース名</param>
        public void DropDb(string DbName)
        {
            string SqlText = string.Format("DROP DATABASE IF EXISTS {0};", DbName);
            CDbTable DbWork = DoSql(false, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            else if (IsDbExists(DbName))
            {
                //同名のDBが存在する場合は削除失敗
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：ロール一覧を取得
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <returns>ロール一覧</returns>
        public List<string> GetRoleList()
        {
            string SqlText = string.Format("SELECT * FROM pg_user;");
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            int index = DbWork.IndexOf("usename");
            List<string> RoleList = new();
            if (0 <= index)
            {
                foreach (List<string> recode in DbWork.Recods)
                {
                    RoleList.Add(recode[index]);
                }
            }
            return RoleList;
        }

        /// <summary>
        /// SQL実行：ロールが存在するか？
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="DbName">ユーザー名</param>
        /// <returns>有無</returns>
        private bool IsRoleExists(string UserName)
        {
            //CreateRoleで作成するとアルファベットが小文字のDB名になる
            return (0 <= GetRoleList().IndexOf(UserName.ToLower()));
        }

        /// <summary>
        /// SQL実行：ロールを作成する
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="UserName">ユーザー名</param>
        /// <param name="Password">パスワード</param>
        public void CreateRole(string UserName, string Password)
        {
            if (IsRoleExists(UserName))
            {
                //同名のロールが存在する
                return;
            }
            string SqlText = string.Format("CREATE ROLE {0} WITH LOGIN PASSWORD '{1}';", UserName, Password);
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            else if (!IsRoleExists(UserName))
            {
                //同名のロールが存在しない場合は作成失敗
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：ロールを削除する
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="UserName">ユーザー名</param>
        public void DropRole(string UserName)
        {
            string SqlText = string.Format("DROP ROLE IF EXISTS {0};", UserName);
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            else if (IsRoleExists(UserName))
            {
                //同名のロールが存在する場合は削除失敗
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：テーブル一覧を取得
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <returns>テーブル一覧</returns>
        public List<string> GetTableList()
        {
            string SqlText = string.Format("SELECT schemaname, tablename, tableowner FROM pg_tables WHERE schemaname NOT LIKE 'pg_%' AND schemaname != 'information_schema';");
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            int index = DbWork.IndexOf("tablename");
            List<string> TableList = new();
            if (0 <= index)
            {
                foreach (List<string> recode in DbWork.Recods)
                {
                    TableList.Add(recode[index]);
                }
            }
            return TableList;
        }

        /// <summary>
        /// SQL実行：テーブルが存在するか？
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <returns>有無</returns>
        private bool IsTableExists(string TableName)
        {
            //CreateTableで作成するとアルファベットが小文字のDB名になる
            return (0 <= GetTableList().IndexOf(TableName.ToLower()));
        }

        /// <summary>
        /// 基本となる行名一覧を取得
        /// </summary>
        /// <returns>基本となる行名一覧</returns>
        private List<MyUI.MyMenu> GetBaseColumns()
        {
            List<MyUI.MyMenu> MenuList = new()
            {
                new MyUI.MyMenu() { Name = "Serial", DbType = "serial", Primary = true },
                new MyUI.MyMenu() { Name = "Update", DbType = "timestamp", Primary = false },
                new MyUI.MyMenu() { Name = "Name", DbType = "text", Primary = false },
                new MyUI.MyMenu() { Name = "Thumbnail", DbType = "text", Primary = false },
            };
            return MenuList;
        }

        /// <summary>
        /// SQL実行：テーブルを作成する
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="MenuList">テーブルの要素</param>
        public void CreateTable(string TableName, List<MyUI.MyMenu> MenuList)
        {
            if (IsTableExists(TableName))
            {
                //同名のテーブルが存在する
                return;
            }
            string ConnectText = "";
            string ColumnText = "";
            List<MyUI.MyMenu> AllMenuList = GetBaseColumns();
            AllMenuList.AddRange(MenuList);
            foreach (MyUI.MyMenu menu in AllMenuList)
            {
                if (menu.Primary)
                {
                    ColumnText += string.Format("{0}{1} {2} PRIMARY KEY", ConnectText, menu.Name, menu.DbType);
                }
                else
                {
                    ColumnText += string.Format("{0}{1} {2}", ConnectText, menu.Name, menu.DbType);
                }
                ConnectText = " , ";
            }
            string SqlText = string.Format("CREATE TABLE {0} ( {1} );", TableName, ColumnText);
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            else if (!IsTableExists(TableName))
            {
                //同名のテーブルが存在しない場合は作成失敗
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：テーブルを削除する
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        public void DropTable(string TableName)
        {
            string SqlText = string.Format("DROP TABLE IF EXISTS {0} RESTRICT;", TableName);
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            else if (IsTableExists(TableName))
            {
                //同名のテーブルが存在する場合は削除失敗
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：テーブルの行名一覧を取得
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <returns>テーブルの行名一覧</returns>
        public List<string> GetTableColumns(string TableName)
        {
            //CreateTableで作成するとアルファベットが小文字のDB名になる
            string SqlText = string.Format("SELECT * FROM information_schema.columns WHERE table_name = '{0}' ORDER BY ordinal_position;", TableName.ToLower());
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            int index = DbWork.IndexOf("column_name");
            List<string> TableColumns = new();
            if (0 <= index)
            {
                foreach (List<string> recode in DbWork.Recods)
                {
                    TableColumns.Add(recode[index]);
                }
            }
            return TableColumns;
        }

        /// <summary>
        /// SQL実行：テーブルに行を追加
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="ColumnName">追加する行の名前</param>
        /// <param name="ColumnType">追加する行のデータ型</param>
        private void AddTableColumn(string TableName, MyUI.MyMenu Column)
        {
            //CreateTableで作成するとアルファベットが小文字のDB名になる
            string PrimaryText = (Column.Primary) ? " PRIMARY KEY" : "";
            string SqlText = string.Format("ALTER TABLE {0} ADD {1} {2}{3};", TableName.ToLower(), Column.Name, Column.DbType, PrimaryText);
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：テーブルに行を削除
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="ColumnName">削除する行の名前</param>
        private void DropTableColumn(string TableName, string ColumnName)
        {
            //CreateTableで作成するとアルファベットが小文字のDB名になる
            string SqlText = string.Format("ALTER TABLE {0} DROP COLUMN {1};", TableName.ToLower(), ColumnName.ToLower());
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：テーブルの要素を変更
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="MenuList">テーブルの要素</param>
        public void ChangeTableColumns(string TableName, List<MyUI.MyMenu> MenuList)
        {
            List<string> TableColums = GetTableColumns(TableName);
            List<MyUI.MyMenu> AllMenuList = GetBaseColumns();
            AllMenuList.AddRange(MenuList);
            foreach (string colum in TableColums)
            {
                //CreateTableで作成するとアルファベットが小文字の要素名になる
                if (AllMenuList.FindIndex(item => item.Name.ToLower().Equals(colum)) < 0)
                {
                    //不要な要素を削除
                    DropTableColumn(TableName, colum);
                }
            }
            TableColums = GetTableColumns(TableName);
            foreach (MyUI.MyMenu menu in AllMenuList)
            {
                //CreateTableで作成するとアルファベットが小文字の要素名になる
                if (TableColums.FindIndex(colum => colum.Equals(menu.Name.ToLower())) < 0)
                {
                    //不足している要素を追加
                    AddTableColumn(TableName, menu);
                }
            }
            TableColums = GetTableColumns(TableName);
            if (!TableColums.Count.Equals(AllMenuList.Count))
            {
                //要素数が一致しない場合は変更に失敗
                ThrowException();
            }
        }

        /// <summary>
        /// SQL実行：レコードを取得
        /// ※SQLの実行に失敗した場合は例外が発生
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="Order">並び順の指定(SQLのORDER BYの記述)</param>
        /// <param name="Filter">条件の指定(SQLのWHEREの記述)</param>
        /// <returns>レコード</returns>
        protected CDbTable SelectRecods(string TableName, string Order = "", string Filter = "")
        {
            //CreateTableで作成するとアルファベットが小文字のDB名になる
            string OrderText = (string.IsNullOrEmpty(Order)) ? "" : string.Format(" ORDER BY {0}", Order);
            string WhereText = (string.IsNullOrEmpty(Filter)) ? "" : string.Format(" WHERE {0}", Filter);
            string SqlText = string.Format("SELECT * FROM {0}{1}{2};", TableName.ToLower(), WhereText, OrderText);
            CDbTable DbWork = DoSql(true, SqlText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
            return DbWork;
        }

        /// <summary>
        ///  SQL実行：レコードを追加
        ///  (注)コマンドが失敗した場合は例外が発生する
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="Columns">行</param>
        /// <param name="Values">データ</param>
        protected void InsertRecod(string TableName, string Columns, string Values)
        {
            string sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2}) RETURNING *;", TableName, Columns, Values);
            CDbTable DbWork = DoSql(true, sql);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
        }

        /// <summary>
        ///  SQL実行：レコードを更新
        ///  (注)コマンドが失敗した場合は例外が発生する
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="Columns">行</param>
        /// <param name="Values">データ</param>
        /// <param name="Filter">条件の指定(SQLのWHEREの記述)</param>
        /// <param name="OldUpdateText">前回の更新日時</param>
        protected void UpdateRecod(string TableName, string Columns, string Values, string Filter, string OldUpdateText)
        {
            string SqlUpdateText = string.Format("SELECT update FROM {0} WHERE {1}", TableName, Filter);
            string sql = string.Format("UPDATE {0} SET ({1}) = ({2}) WHERE {3} RETURNING *;", TableName, Columns, Values, Filter);
            CDbTable DbWork = DoSql(true, sql, SqlUpdateText, OldUpdateText);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
        }

        /// <summary>
        ///  SQL実行：レコードを削除
        ///  (注)コマンドが失敗した場合は例外が発生する
        /// </summary>
        /// <param name="TableName">テーブル名</param>
        /// <param name="Filter">条件の指定(SQLのWHEREの記述)</param>
        protected void DeleteRecod(string TableName, string Filter)
        {
            string sql = string.Format("DELETE FROM {0} WHERE {1} RETURNING *;", TableName, Filter);
            CDbTable DbWork = DoSql(true, sql);
            if (!DbWork.Enable)
            {
                ThrowException();
            }
        }
    }
}
