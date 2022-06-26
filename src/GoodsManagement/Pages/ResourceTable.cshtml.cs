using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using MyUtility;
using MyEf;

namespace GoodsManagement.Pages
{
    public class ResourceTableModel : PageModel
    {
        public readonly MyUI.MyTablePage PageConfig = new()
        {
            Result = "",
            MenuType = new(),
            DateText = "",
            DeleteEnable = "disabled",
        };
        private readonly MyJsonLoader<List<MyUI.MyResource>> Resource = new("Pages/MyConfig/resource.json");

        /// <summary>
        /// Get
        /// </summary>
        public void OnGet()
        {
            if (string.IsNullOrEmpty(PageConfig.Name))
            {
                //OnPostから呼ばれたときは通らない処理
                PageConfig.Name = Request.Query["name"];
            }
            if (!string.IsNullOrEmpty(Request.Query["delete"]))
            {
                PageConfig.DeleteEnable = "";
            }
            if (!Resource.Load())
            {
                MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
            }
            MyJsonLoader<MyUI.MyConfig> Config = new(Resource.Data.Find(item => item.Name.Equals(PageConfig.Name)).Config);
            if (!Config.Load())
            {
                MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
            }
            //アイテムに設定する項目一覧を設定
            PageConfig.MenuType.AddRange(Config.Data.MenuType);
            PageConfig.TitelText = PageConfig.Name;
            PageConfig.MessageText = Resource.Data.Find(item => item.Name.Equals(PageConfig.Name)).Title;
            if (string.IsNullOrEmpty(PageConfig.DateText))
            {
                PageConfig.DateText = Config.GetDateText();
            }
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <returns></returns>
        public ActionResult OnPost()
        {
            PageConfig.Name = Request.Form["ResourceName"];
            PageConfig.DateText = Request.Form["DateText"];
            if (!IsSameCount())
            {
                return Redirect(string.Format("./ResourceTable?name={0}", PageConfig.Name));
            }
            OnGet();
            MyJsonLoader<MyUI.MyConfig> Config = new(Resource.Data.Find(item => item.Name.Equals(PageConfig.Name)).Config);
            if (!Config.Load())
            {
                MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
            }
            Config.Data.MenuType = new();
            for (int i = 0; i < Request.Form["Name"].Count; i++)
            {
                if (!string.IsNullOrEmpty(Request.Form["Name"][i]) &&
                    !string.IsNullOrEmpty(Request.Form["View"][i]))
                {
                    MyUI.MyMenu NewMenu = new() {
                        Name = Request.Form["Name"][i],
                        DbType = "text",
                        Primary = false,
                        View = Request.Form["View"][i].Equals("〇"),
                        Option = null,
                    };
                    List<string> Option = new();
                    string OptionText = Request.Form["Option"][i];
                    string[] OptionList = OptionText.Replace("\r", "").Split('\n');
                    foreach (string opt in OptionList)
                    {
                        string optItem = opt.Trim();
                        if (0 < optItem.Length)
                        {
                            Option.Add(optItem);
                        }
                    }
                    if(0 < Option.Count)
                    {
                        NewMenu.Option = Option;
                    }
                    Config.Data.MenuType.Add(NewMenu);
                }
            }
            if (!Config.Save(PageConfig.DateText))
            {
                MyUtilityLog.ThrowException(string.Format("Failed to save json file."));
            }
            //データベースを更新
            MyGoodsDbContext MyDbHost = new();
            MyDbHost.ChangeTableColumns(Config.Data.TableName, Config.Data.MenuType);
            return Redirect("./ResourceDb");
        }

        /// <summary>
        /// データ作成に使用する要素が同じ数か？
        /// </summary>
        /// <returns>判定結果</returns>
        private bool IsSameCount()
        {
            List<string> DataLabel = new()
            {
                "OldName",
                "View",
                "Option",
            };
            foreach (string label in DataLabel)
            {
                if (!Request.Form["Name"].Count.Equals(Request.Form[label].Count))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
