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
    public class ResourceEditModel : PageModel
    {
        public readonly MyUI.MyResourcePage PageConfig = new();
        private readonly MyGoodsDbContext MyDbHost = new();
        private readonly MyJsonLoader<List<MyUI.MyResource>> Resource = new("Pages/MyConfig/resource.json");
        public MyJsonLoader<MyUI.MyConfig> ResourceConfig;

        /// <summary>
        /// Get
        /// </summary>
        public void OnGet()
        {
            if(string.IsNullOrEmpty(PageConfig.Name))
            {
                //OnPostから呼ばれたときは通らない処理
                string Edit = Request.Query["edit"];
                PageConfig.Name = Request.Query["name"];
                PageConfig.Serial = Request.Query["serial"];
                PageConfig.Edit = (string.IsNullOrEmpty(Edit)) ? false : true;
            }
            if (!Resource.Load())
            {
                MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
            }
            else
            {
                //登録しているアイテムの情報を設定
                ResourceConfig = new(Resource.Data.Find(item => item.Name.Equals(PageConfig.Name)).Config);
                if (!ResourceConfig.Load())
                {
                    MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
                }
                if (string.IsNullOrEmpty(PageConfig.Serial))
                {
                    //シリアルNoが付与されていない場合は未登録のアイテム
                    //入力欄を初期値に設定
                    MyUI.MySummary MyItem = new()
                    {
                        Serial = "",
                        Name = "",
                        Thumbnail = "",
                        Explain = new(),
                    };
                    foreach (MyUI.MyMenu menu in ResourceConfig.Data.MenuType)
                    {
                        MyItem.Explain.Add(new() {
                            Name = menu.Name,
                            Value = "",
                        });
                    }
                    ResourceConfig.Data.MenuItem.Add(MyItem);
                }
                else
                {
                    //シリアルNoが付与されている場合は登録済みのアイテム
                    //入力欄を現在の登録している値に設定
                    List<MyUI.MySummary> MyInfoList = MyDbHost.GetInfoList(ResourceConfig.Data.TableName, ResourceConfig.Data.MenuType, PageConfig.Serial);
                    ResourceConfig.Data.MenuItem.Add(MyInfoList.Find(item => item.Serial.Equals(PageConfig.Serial)));
                }
            }
            PageConfig.TitelText = PageConfig.Name;
            PageConfig.MessageText = Resource.Data.Find(item => item.Name.Equals(PageConfig.Name)).Title;
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <returns></returns>
        public ActionResult OnPost()
        {
            string Edit = Request.Query["edit"];
            PageConfig.Name = Request.Form["ResourceName"];
            PageConfig.Serial = Request.Form["Serial"];
            if (string.IsNullOrEmpty(Edit))
            {
                return Redirect(string.Format("./ResourceEdit?name={0}&serial={1}&edit=on", PageConfig.Name, PageConfig.Serial));
            }
            OnGet();
            //アイテムの情報を登録で使用する形式で作成
            MyUI.MySummary MyItem = new()
            {
                Serial = PageConfig.Serial,
                Update = DateTime.Now,
                Name = Request.Form["Name"],
                Thumbnail = Request.Form["Thumbnail"],
                Explain = new(),
            };
            foreach (MyUI.MyMenu menu in ResourceConfig.Data.MenuType)
            {
                MyItem.Explain.Add(new()
                {
                    Name = menu.Name,
                    Value = Request.Form[menu.Name],
                });
            }
            //アイテムの情報を登録 or 更新
            MyDbHost.UpdateInfoList(ResourceConfig.Data.TableName, MyItem, DateTime.Parse(Request.Form["Update"]));
            return Redirect(string.Format("./ResourceList?name={0}", PageConfig.Name));
        }
    }
}
