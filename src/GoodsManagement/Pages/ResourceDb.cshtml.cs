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
    public class ResourceDbModel : PageModel
    {
        public readonly MyUI.MySettingPage PageConfig = new() {
            Result = "",
            ResourceList = new(),
            DateText = "",
            DeleteEnable = "disabled",
        };
        private readonly MyJsonLoader<List<MyUI.MyResource>> Resource = new("Pages/MyConfig/resource.json");

        /// <summary>
        /// Get
        /// </summary>
        public void OnGet()
        {
            if (!string.IsNullOrEmpty(Request.Query["delete"]))
            {
                PageConfig.DeleteEnable = "";
            }
            if (!Resource.Load())
            {
                MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
            }
            foreach (MyUI.MyResource item in Resource.Data)
            {
                //リソースの一覧を設定
                MyJsonLoader<MyUI.MyConfig> Config = new(item.Config);
                if (!Config.Load())
                {
                    MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
                }
                MyUI.MySettingBase setting = new()
                {
                    Name = item.Name,
                    Title = item.Title,
                    TableName = Config.Data.TableName,
                };
                PageConfig.ResourceList.Add(setting);
            }
            PageConfig.TitelText = "Setting";
            PageConfig.MessageText = "設定";
            if (string.IsNullOrEmpty(PageConfig.DateText))
            {
                PageConfig.DateText = Resource.GetDateText();
            }
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <returns></returns>
        public ActionResult OnPost()
        {
            PageConfig.DateText = Request.Form["DateText"];
            if (IsSameValue(Request.Form["Name"]) ||
                IsSameValue(Request.Form["Title"]) ||
                IsSameValue(Request.Form["TableName"]) ||
                !IsSameCount())
            {
                return Redirect("./ResourceDb");
            }
            Resource.Data = new();
            for (int i = 0; i < Request.Form["Name"].Count; i++)
            {
                if (!string.IsNullOrEmpty(Request.Form["Name"]) &&
                    !string.IsNullOrEmpty(Request.Form["Title"]) &&
                    !string.IsNullOrEmpty(Request.Form["TableName"]))
                {
                    //項目が空白 or nullでなければデータ更新
                    string ConfigPath = string.Format("Pages/MyConfig/resource/{0}.json", Request.Form["Name"][i]);
                    Resource.Data.Add(new()
                    {
                        Name = Request.Form["Name"][i],
                        Title = Request.Form["Title"][i],
                        Config = ConfigPath,
                    });
                    MyJsonLoader<MyUI.MyConfig> Config = new(ConfigPath);
                    if (!Config.Load())
                    {
                        //設定ファイルが存在しない場合は新規作成
                        Config.Data = new()
                        {
                            TableName = Request.Form["TableName"][i],
                            MenuType = new(),
                            MenuItem = new(),
                        };
                        if (!Config.Save(Config.GetDateText()))
                        {
                            MyUtilityLog.ThrowException(string.Format("Failed to save json file."));
                        }
                    }
                    else if (string.IsNullOrEmpty(Config.Data.TableName))
                    {
                        //設定ファイルが存在している場合はテーブル名がない場合だけ更新
                        Config.Data.TableName = Request.Form["TableName"][i];
                        if (!Config.Save(Config.GetDateText()))
                        {
                            MyUtilityLog.ThrowException(string.Format("Failed to save json file."));
                        }
                    }
                }
            }
            if (!Resource.Save(PageConfig.DateText))
            {
                MyUtilityLog.ThrowException(string.Format("Failed to save json file."));
            }
            foreach (MyUI.MyResource resorce in Resource.Data)
            {
                //データベースを更新
                MyJsonLoader<MyUI.MyConfig> Config = new(resorce.Config);
                if (!Config.Load())
                {
                    MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
                }
                MyGoodsDbContext MyDbHost = new();
                MyDbHost.CreateDb(MyDbHost.DataBase);
                MyDbHost.CreateTable(Config.Data.TableName, Config.Data.MenuType);
                MyDbHost.ChangeTableColumns(Config.Data.TableName, Config.Data.MenuType);
            }
            return Redirect("./ResourceDb");
        }

        /// <summary>
        /// 同一の値が存在する？
        /// </summary>
        /// <param name="ElementList">要素一覧</param>
        /// <returns></returns>
        private bool IsSameValue(Microsoft.Extensions.Primitives.StringValues Values)
        {
            foreach (string value in Values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (1 < Values.Count(v => v.Equals(value)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// データ作成に使用する要素が同じ数か？
        /// </summary>
        /// <returns>判定結果</returns>
        private bool IsSameCount()
        {
            List<string> DataLabel = new()
            {
                "Title",
                "TableName",
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
