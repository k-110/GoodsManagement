using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using System.Text;

using MyUtility;
using MyEf;

namespace GoodsManagement.Pages
{
    public class ResourceListModel : PageModel
    {
        public readonly MyUI.MyResourcePage PageConfig = new();
        private readonly MyGoodsDbContext MyDbHost = new();
        private readonly MyJsonLoader<List<MyUI.MyResource>> Resource = new("Pages/MyConfig/resource.json");
        public MyJsonLoader<MyUI.MyConfig> ResourceConfig;
        private List<MyUI.MySummary> AllItemList;

        /// <summary>
        /// Get
        /// </summary>
        public void OnGet()
        {
            const string CookieKey = "ResourceList.Filter";
            if (string.IsNullOrEmpty(PageConfig.Name))
            {
                //OnPost����Ă΂ꂽ�Ƃ��͒ʂ�Ȃ�����
                PageConfig.Name = Request.Query["name"];
                PageConfig.Filter = Request.Cookies[CookieKey];
                if (string.IsNullOrEmpty(PageConfig.Filter))
                {
                    PageConfig.Filter = "";
                }
            }
            if (!Resource.Load())
            {
                MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
            }
            else
            {
                //�A�C�e���̈ꗗ�����쐬
                ResourceConfig = new(Resource.Data.Find(item => item.Name.Equals(PageConfig.Name)).Config);
                if (!ResourceConfig.Load())
                {
                    MyUtilityLog.ThrowException(string.Format("Failed to read json file."));
                }
                AllItemList = MyDbHost.GetInfoList(ResourceConfig.Data.TableName, ResourceConfig.Data.MenuType);
                if (0 < PageConfig.Filter.Length)
                {
                    //�t�B���^�[���ݒ肳��Ă���ꍇ�͊Y������A�C�e���݂̂ňꗗ�쐬
                    string FilterText = PageConfig.Filter.ToLower();
                    ResourceConfig.Data.MenuItem = new();
                    foreach (MyUI.MySummary item in AllItemList)
                    {
                        bool HitFilter = false;
                        foreach (MyUI.MyExplain exp in item.Explain)
                        {
                            if (exp.Value.ToLower().Contains(FilterText))
                            {
                                HitFilter = true;
                                break;
                            }
                        }
                        if (item.Serial.ToLower().Contains(FilterText) ||
                            item.Update.ToString().Contains(FilterText) ||
                            item.Name.ToLower().Contains(FilterText) ||
                            item.Thumbnail.ToLower().Contains(FilterText) ||
                            HitFilter)
                        {
                            ResourceConfig.Data.MenuItem.Add(item);
                        }
                    }
                }
                else
                {
                    //�t�B���^�[���ݒ肳��Ă��Ȃ��ꍇ�͑S�A�C�e���ňꗗ�쐬
                    ResourceConfig.Data.MenuItem = AllItemList;
                }
            }
            PageConfig.TitelText = PageConfig.Name;
            PageConfig.MessageText = Resource.Data.Find(item => item.Name.Equals(PageConfig.Name)).Title;
            //�t�B���^�[��Cookie�ɕۑ�
            Response.Cookies.Append(CookieKey, PageConfig.Filter);
        }

        //Post
        public ActionResult OnPost()
        {
            PageConfig.Name = Request.Form["ResourceName"];
            PageConfig.Filter = Request.Form["Filter"];
            if (PageConfig.Filter == null)
            {
                //OnGet�����ʂŎg�p���邽�߂Ƀf�[�^�����Ă���
                const string CookieKey = "ResourceList.Filter";
                PageConfig.Filter = Request.Cookies[CookieKey];
            }
            OnGet();
            if (Request.Query["action"].Equals("DownloadButton_Clicked"))
            {
                //�ꗗDL�̏ꍇ�ɒʂ鏈��
                return DownloadButton_Clicked();
            }
            else if (Request.Query["action"].Equals("UploadButton_Clicked"))
            {
                //�ꊇULL�̏ꍇ�ɒʂ鏈��
                UploadButton_Clicked();
            }
            return Redirect(string.Format("./ResourceList?name={0}", PageConfig.Name));
        }

        /// <summary>
        /// �_�E�����[�h
        /// </summary>
        /// <returns></returns>
        private ActionResult DownloadButton_Clicked()
        {
            string LabelText = "";

            //CSV�t�@�C����1�s�ڂ��쐬
            if (0 < ResourceConfig.Data.MenuItem.Count)
            {
                foreach (MyUI.MyExplain explain in ResourceConfig.Data.MenuItem[0].Explain)
                {
                    LabelText += string.Format("{0},", explain.Name);
                }
            }
            string FileText = string.Format("Serial,Name,Update,Thumbnail,{0}\n", LabelText);

            //CSV�t�@�C����2�s�ڈȍ~���쐬
            foreach (MyUI.MySummary item in ResourceConfig.Data.MenuItem)
            {
                string ItemCsv = "";
                ItemCsv += string.Format("{0},{1},{2},{3},", item.Serial, item.Name, item.Update, item.Thumbnail);
                foreach(MyUI.MyExplain explain in item.Explain)
                {
                    ItemCsv += string.Format("{0},", explain.Value);
                }
                FileText += ItemCsv + "\n";
            }

            //UTF8�`���Ńt�@�C���𑗐M
            byte[] FileData = Encoding.UTF8.GetBytes(FileText);
            return File(FileData, MediaTypeNames.Text.Plain, string.Format("{0}_{1}.csv", DateTime.Now.ToString("yyyyMMdd"), PageConfig.Name));
        }

        /// <summary>
        /// �A�b�v���[�h
        /// </summary>
        private void UploadButton_Clicked()
        {
            List<MyUI.MySummary> CsvItemList = new();
            try
            {
                IFormFile CsvFile = Request.Form.Files["CsvFile"];
                if (!string.IsNullOrEmpty(CsvFile.FileName))
                {
                    //Post���ꂽ�t�@�C���̒��g�����o��
                    byte[] CsvByte;
                    using (System.IO.Stream rs = CsvFile.OpenReadStream())
                    {
                        CsvByte = new byte[rs.Length];
                        rs.Read(CsvByte, 0, CsvByte.Length);
                        rs.Close();
                    }

                    //�t�@�C���̒��g����͂���DB�X�V�p�̃f�[�^���쐬
                    string[] CsvText = Encoding.UTF8.GetString(CsvByte).Replace("\r", "").Split('\n');
                    string[] NameList = CsvText[0].Split(',');
                    int IndexSerial = Array.FindIndex(NameList, item => item.Equals("Serial"));
                    for (int i = 1; i < CsvText.Length; i++)
                    {
                        if (string.IsNullOrEmpty(CsvText[i]))
                        {
                            continue;
                        }
                        string[] ValueList = CsvText[i].Split(',');
                        MyUI.MySummary summary = AllItemList.Find(item => item.Serial.Equals(ValueList[IndexSerial]));
                        if (summary == null)
                        {
                            summary = new() {
                                Serial = "",
                                //Update = DateTime.Now,
                                Name = "",
                                Thumbnail = "",
                                Explain = new(),
                            };
                        }
                        foreach (string name in NameList)
                        {
                            int IndexValue = Array.FindIndex(NameList, item => item.Equals(name));
                            if (string.IsNullOrEmpty(name) || (IndexValue < 0))
                            {
                                continue;
                            }
                            switch (name)
                            {
                                case "Serial":
                                case "Update":
                                    //�������Ȃ�
                                    break;
                                case "Name":
                                    summary.Name = ValueList[IndexValue];
                                    break;
                                case "Thumbnail":
                                    summary.Thumbnail = ValueList[IndexValue];
                                    break;
                                default:
                                    if (string.IsNullOrEmpty(summary.Serial))
                                    {
                                        //�V�K���R�[�h��ǉ�����ꍇ
                                        summary.Explain.Add(new() {
                                            Name = name,
                                            Value = ValueList[IndexValue],
                                        });
                                    }
                                    else
                                    {
                                        //�����̃��R�[�h���X�V����ꍇ
                                        summary.Explain.Find(exp => exp.Name.Equals(name)).Value = ValueList[IndexValue];
                                    }
                                    break;
                            }
                        }
                        CsvItemList.Add(summary);
                    }

                }
            }
            catch (Exception ex)
            {
                MyUtilityLog.ThrowException(ex.ToString());
            }

            //DB���X�V
            foreach (MyUI.MySummary summary in CsvItemList)
            {
                MyDbHost.UpdateInfoList(ResourceConfig.Data.TableName, summary, summary.Update);
            }
        }
    }
}
