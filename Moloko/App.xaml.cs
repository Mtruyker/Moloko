using System.Windows;
using Moloko.Services;

namespace Moloko;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var store = AppDataStoreFactory.Create();
        var loginWindow = new LoginWindow(store);
        var loginResult = loginWindow.ShowDialog();

        if (loginResult == true && loginWindow.AuthenticatedUser is not null)
        {
            var mainWindow = new MainWindow(store, loginWindow.AuthenticatedUser);
            MainWindow = mainWindow;
            mainWindow.Show();
            return;
        }

        Shutdown();
    }
}
