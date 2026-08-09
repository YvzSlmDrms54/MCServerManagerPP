using System.Windows;

namespace MCServerManagerPP;

public partial class MaintenanceWindow : Window
{
    private readonly ServerManager _server;

    public MaintenanceWindow(ServerManager server)
    {
        InitializeComponent();
        _server = server;

        Title = Lang.Get("maintenance_title");
        LblDescription.Text = Lang.Get("maintenance_description");

        RefreshUI();
    }

    private void RefreshUI()
    {
        bool active = _server.Config.MaintenanceModeActive;

        LblStatus.Text = active ? Lang.Get("maintenance_status_on") : Lang.Get("maintenance_status_off");
            LblStatus.Foreground = active
        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xCC, 0x66)) // uyarı sarısı
        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7E, 0xD8, 0x83)); // BrushGreenBright

        BtnToggle.Content = active ? Lang.Get("maintenance_disable") : Lang.Get("maintenance_enable");
    }

    private void BtnToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_server.Config.MaintenanceModeActive)
        {
            _server.DisableMaintenanceMode();
        }
        else
        {
            _server.EnableMaintenanceMode();
        }

        RefreshUI();
    }
}