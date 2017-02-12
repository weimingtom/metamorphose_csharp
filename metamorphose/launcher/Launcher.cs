using System;
using System.Windows.Forms;

namespace metamorphose
{
    class Launcher
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form form1 = new Form();
            form1.Text = "hello";
            Application.Run(form1);
        }
    }
}
