using System;
using System.Collections.Generic;
using System.Text;

#if !BRAIN_CARD_DISABLE_XAML_ISLANDS
using MyUWPApp;
#endif

namespace BrainCard
{
    public class Program
    {
        [System.STAThreadAttribute()]
        public static void Main(string[] args = null)
        {
#if !BRAIN_CARD_DISABLE_XAML_ISLANDS
            using (new MyUWPApp.App())
            {
                var app = new App(args);
                app.InitializeComponent();
                app.Run();
            }
#else
            var app = new App(args);
            app.InitializeComponent();
            app.Run();
#endif
        }
    }
}
