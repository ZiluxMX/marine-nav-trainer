using System.Configuration;
using System.Data;
using System.Windows;

namespace marine_nav_trainer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppPaths.EnsureSeeded();
        }
    }

}
