using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace BrainCard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        readonly string[] Args;

        public App(string[] args = null)
        {
            Args = args;
            // XAML Islands initialization is centralized in SubWindow.
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow(Args);
            mainWindow.Show();
        }
    }
}
