using System;
using System.Collections.Generic;
using System.Text;
using MyUWPApp;

namespace BrainCard
{
    public class Program
    {
        [System.STAThreadAttribute()]
        public static void Main(string[] args = null)
    {
            using (new MyUWPApp.App())
            {
                var app = new App(args);
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
