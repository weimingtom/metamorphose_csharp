using System;
using System.Windows.Forms;
using metamorphose.test;

namespace metamorphose
{
    class Launcher
    {
        [STAThread]
        static void Main(string[] args)
        {
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            Form form1 = new Form();
//            form1.Text = "hello";
            if (false)
            {
            	new Test001();
            }
            else
            {
            	new Runner(new string[]{"fib.lua"}, "runner");
            }
            //Application.Run(form1);
            Console.ReadKey();
        }
    }
}
