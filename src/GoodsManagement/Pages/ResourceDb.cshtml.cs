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
                //���\�[�X�̈ꗗ��ݒ�
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
            PageConfig.MessageText = "�ݒ�";
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
                    //���ڂ��� or null�łȂ���΃f�[�^�X�V
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
                        //�ݒ�t�@�C�������݂��Ȃ��ꍇ�͐V�K�쐬
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
                        //�ݒ�t�@�C�������݂��Ă���ꍇ�̓e�[�u�������Ȃ��ꍇ�����X�V
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
                //�f�[�^�x�[�X���X�V
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
        /// ����̒l�����݂���H
        /// </summary>
        /// <param name="ElementList">�v�f�ꗗ</param>
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
        /// �f�[�^�쐬�Ɏg�p����v�f�����������H
        /// </summary>
        /// <returns>���茋��</returns>
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
