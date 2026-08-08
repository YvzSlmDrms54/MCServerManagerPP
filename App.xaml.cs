using System.Windows;

namespace MCServerManagerPP;

using Application = System.Windows.Application;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var tempServer = new ServerManager();

        if (string.IsNullOrEmpty(tempServer.Config.Language))
        {
            var langWindow = new LanguageSelectWindow();
            bool? result = langWindow.ShowDialog();

            if (result == true)
            {
                tempServer.Config.Language = langWindow.SelectedLanguage;
                tempServer.SaveConfig();
            }
        }

        Lang.CurrentLanguage = string.IsNullOrEmpty(tempServer.Config.Language) ? "tr" : tempServer.Config.Language;

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}