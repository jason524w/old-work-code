using AeroEduLib;
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Booth_Camera
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string applicationPath = System.AppDomain.CurrentDomain.BaseDirectory;
            bool noInstance;
            Mutex mutex = new Mutex(true, "Global\\" + Assembly.GetExecutingAssembly().FullName, out noInstance);
            if (noInstance)
            {
                if (GetSystemInfo.GetLoaclMac() == string.Empty)
                {
                    MessageBox.Show("未发现本地网卡，程序未能启动。");
                    return;
                }
                string _license = Setting.GetLicense();
                bool checkLicense = GetSystemInfo.CheckLicense(_license);
                if (!checkLicense)
                {
                    // 弹出注册窗口
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new frmReg());
                }
                else 
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new frmMain1());
                    mutex.ReleaseMutex();
                }
            }
            else
            {
                MessageBox.Show("程序已经启动，请不要同时运行多次。");
                Application.Exit();
            }
        }
    }
}