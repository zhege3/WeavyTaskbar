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
                    MessageBox.Show("\u7a0b\u5e8f\u5df2\u5728\u8fd0\u884c\u4e2d\u3002", "WeavyTaskbar",
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
