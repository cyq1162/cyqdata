using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CYQ.Data.Table;
using System.Threading;

namespace CYQ.Data.ProjectTool
{
    static class Program
    {
        private static int _IsEnglish;
        /// <summary>
        /// 当前是否英语环境
        /// </summary>
        public static bool IsEnglish
        {
            get
            {
                if (_IsEnglish == -1)
                {
                    if (Thread.CurrentThread.CurrentCulture.Name.StartsWith("zh-"))
                    {
                        _IsEnglish = 0;
                    }
                    else
                    {
                        _IsEnglish = 1;
                    }
                }
                return _IsEnglish == 1;
            }
        }

        internal static string path = string.Empty;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] para)
        {

            if (para.Length > 0)
            {
                path = para[0].TrimEnd('"');
            }
            else
            {
                path = AppDomain.CurrentDomain.BaseDirectory;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.Run(new OpForm());
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Application_ThreadException(sender, null);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception error = e.Exception as Exception;
            MessageBox.Show("UnhandledException ：" + error.Message, "System Tip!");
        }
    }
}