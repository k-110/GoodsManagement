using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MyUtility;

namespace GoodsManagement.Pages
{
    public class IndexModel : PageModel
    {
        public readonly MyUI.MyIndexPage PageConfig = new() {
            TitelText = "Home",
            MessageText = "項目一覧",
            ItemName = "項目名",
            MenuItem = new(),
        };
        public readonly MyJsonLoader<List<MyUI.MyResource>> Resource = new("Pages/MyConfig/resource.json");

        /// <summary>
        /// Get
        /// </summary>
        public void OnGet()
        {
            if (!Resource.Load())
            {
                MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
            }
            foreach (MyUI.MyResource resource in Resource.Data)
            {
                //リソースの一覧を設定
                PageConfig.MenuItem.Add(new MyUI.MyButton() {
                    Name = resource.Title,
                    Link = string.Format("./ResourceList?name={0}", resource.Name),
                });
            }
        }
    }
}
