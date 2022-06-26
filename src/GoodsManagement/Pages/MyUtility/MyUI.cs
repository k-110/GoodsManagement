using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyUtility
{
    public class MyUI
    {
        /// <summary>
        /// ボタン
        /// </summary>
        public class MyButton
        {
            public string Name { get; set; }
            public string Link { get; set; }
        }

        /// <summary>
        /// 基本となるページ設定
        /// </summary>
        public class MyBasePage
        {
            public string TitelText { get; set; }
            public string MessageText { get; set; }
        }

        /// <summary>
        /// indexのページ設定
        /// </summary>
        public class MyIndexPage : MyBasePage
        {
            public string ItemName { get; set; }
            public List<MyUI.MyButton> MenuItem { get; set; }
        }

        /// <summary>
        /// Resourceのページ設定
        /// </summary>
        public class MyResourcePage : MyBasePage
        {
            public string Name { get; set; }
            public string Filter { get; set; }
            public string Serial { get; set; }
            public bool Edit { get; set; }
        }

        /// <summary>
        /// 設定のページ設定
        /// </summary>
        public class MySettingPage : MyBasePage
        {
            public string Result { get; set; }
            public string DateText { get; set; }
            public string DeleteEnable { get; set; }
            public List<MySettingBase> ResourceList { get; set; }
        }

        /// <summary>
        /// テーブル要素のページ設定
        /// </summary>
        public class MyTablePage : MyBasePage
        {
            public string Name { get; set; }
            public string Result { get; set; }
            public string DeleteEnable { get; set; }
            public string DateText { get; set; }
            public List<MyUI.MyMenu> MenuType { get; set; }
        }

        /// <summary>
        /// 設定の基本情報
        /// </summary>
        public class MySettingBase
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public string TableName { get; set; }
        }

        /// <summary>
        /// 名前と設定の紐づけ
        /// </summary>
        public class MyResource
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public string Config { get; set; }
        }

        /// <summary>
        /// 設定
        /// </summary>
        public class MyConfig
        {
            public string TableName { get; set; }
            public List<MyUI.MyMenu> MenuType { get; set; }
            public List<MyUI.MySummary> MenuItem { get; set; }
        }

        /// <summary>
        /// 要約の種別
        /// </summary>
        public class MyMenu
        {
            public string Name { get; set; }
            public string DbType { get; set; }
            public bool Primary { get; set; }
            public List<string> Option { get; set; }
            public bool View { get; set; }
        }
        
        /// <summary>
        /// 要約の要素
        /// </summary>
        public class MyExplain
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        /// <summary>
        /// 要約
        /// </summary>
        public class MySummary
        {
            public string Serial { get; set; }
            public DateTime Update { get; set; }
            public string Name { get; set; }
            public string Thumbnail { get; set; }
            public List<MyExplain> Explain { get; set; }
        }
    }
}
