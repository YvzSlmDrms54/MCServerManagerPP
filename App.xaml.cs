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

        LoadTheme(tempServer.Config.Theme);

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    public static void LoadTheme(string themeName)
    {
        string fileName = themeName == "light" ? "Theme.Light.xaml" : "Theme.Dark.xaml";
        var dict = new ResourceDictionary { Source = new Uri(fileName, UriKind.Relative) };

        Current.Resources.MergedDictionaries.Clear();
        Current.Resources.MergedDictionaries.Add(dict);
    }
}