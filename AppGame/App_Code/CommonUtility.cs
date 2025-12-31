using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AppGame.App_Code
{
    public class CommonUtility
    {
        public void Txt(string ex)
        {
            //string sourcePath = @"C:\Website\APPGame\Log" + @"\" + DateTime.Now.ToString("yyyy-MM-dd") + "狀態.txt";       //正式
            string sourcePath = @"C:\Website\APPGame\Log\" + DateTime.Now.ToString("yyyy-MM-dd") + "狀態.txt";     //測試
            //string sourcePath = @"E:\POJHIH\Website\APPGame\Log\" + DateTime.Now.ToString("yyyy-MM-dd") + "狀態.txt";    //本地
            #region  寫入txt檔
            string txt = "";
            if (File.Exists(sourcePath))
            {
                #region 取TXT檔內文串
                StreamReader str = new StreamReader(sourcePath);
                //str.ReadLine(); (一行一行讀取)
                txt = str.ReadToEnd();//(一次讀取全部)
                str.Close(); //(關閉str)
                #endregion
                #region 寫入txt檔
                using (StreamWriter sw = new StreamWriter(sourcePath))
                {
                    sw.WriteLine(txt + DateTime.Now + "-" + ex);
                    sw.Close();
                }
                #endregion
            }
            else
            {
                #region 創建txt檔
                FileStream fileStream = new FileStream(sourcePath, FileMode.Create);
                fileStream.Close();
                #endregion
                #region 寫入txt檔
                using (StreamWriter sw = new StreamWriter(sourcePath))
                {
                    sw.WriteLine(DateTime.Now + "-" + ex);
                    sw.Close();
                }
                #endregion
            }
            #endregion
        }
    }
}