using System;
using System.Threading;
using System.Windows.Forms;

namespace WeavyTaskbar
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, "WeavyTaskbar_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("WeavyTaskbar is already running.", "WeavyTaskbar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => { };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { };

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var ctx = new AppContext())
                {
                    try
                    {
                        Application.Run(ctx);
                    }
                    finally
                    {
                        ctx.Cleanup();
                    }
                }
            }
        }
    }
}
