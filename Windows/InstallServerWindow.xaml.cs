using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MCServerManagerPP;

public partial class InstallServerWindow : Window
{
    private readonly ServerManager _server;

    public InstallServerWindow(ServerManager server)
    {
        InitializeComponent();
        _server = server;

        Title = Lang.Get("install_choose_title");
        LblChooseTitle.Text = Lang.Get("install_choose_title");
        BtnVanilla.Content = Lang.Get("install_vanilla");
        BtnPaper.Content = Lang.Get("install_paper");
        BtnFabric.Content = Lang.Get("install_fabric");
        BtnForge.Content = Lang.Get("install_forge");

        _server.OnLogReceived += OnInstallProgress;
        Closed += (s, e) => _server.OnLogReceived -= OnInstallProgress;
    }

    private void OnInstallProgress(string line)
    {
        if (line.Contains("[KURULUM]"))
        {
            Dispatcher.Invoke(() =>
            {
                LblInstallStatus.Text = line.Replace("[KURULUM]", "").Trim();
            });
        }
    }

    private async void BtnVanilla_Click(object sender, RoutedEventArgs e)
    {
        await RunInstall(() => _server.InstallVanillaServer());
    }

    private async void BtnPaper_Click(object sender, RoutedEventArgs e)
    {
        await RunInstall(() => _server.InstallPaperServer());
    }

    private async void BtnFabric_Click(object sender, RoutedEventArgs e)
    {
        await RunInstall(() => _server.InstallFabricServer());
    }

    private void BtnForge_Click(object sender, RoutedEventArgs e)
    {
        ThemedMessageBox.Show("Forge kurulumu yakında eklenecek.", "MCServerManager++", ThemedMessageBoxButtons.Ok, this);
    }

    private async System.Threading.Tasks.Task RunInstall(System.Func<System.Threading.Tasks.Task<string>> installAction)
    {
        ChoicePanel.Visibility = Visibility.Collapsed;
        InstallingPanel.Visibility = Visibility.Visible;
        LblInstalling.Text = Lang.Get("install_installing");

        string result = await installAction();

        ThemedMessageBox.Show(result, "MCServerManager++", ThemedMessageBoxButtons.Ok, this);
        DialogResult = true;
        Close();
    }
}