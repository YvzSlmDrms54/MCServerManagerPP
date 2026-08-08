using System.Windows;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace MCServerManagerPP;

public partial class ScheduledTasksWindow : Window
{
    private readonly ServerManager _server;

    public ScheduledTasksWindow(ServerManager server)
    {
        InitializeComponent();
        _server = server;

        Title = Lang.Get("scheduled_tasks_title");
        LblRestartHeader.Text = Lang.Get("scheduled_restart_header");
        LblRestartTime.Text = Lang.Get("scheduled_time");
        ChkRestartEnabled.Content = Lang.Get("scheduled_enable");
        BtnApplyRestart.Content = Lang.Get("scheduled_apply");

        LblMaintenanceHeader.Text = Lang.Get("scheduled_maintenance_header");
        LblMaintenanceStart.Text = Lang.Get("scheduled_start_time");
        LblMaintenanceEnd.Text = Lang.Get("scheduled_end_time");
        ChkMaintenanceEnabled.Content = Lang.Get("scheduled_enable");
        BtnApplyMaintenance.Content = Lang.Get("scheduled_apply");

        FillHourMinuteCombo(ComboRestartHour, ComboRestartMinute);
        FillHourMinuteCombo(ComboMaintStartHour, ComboMaintStartMinute);
        FillHourMinuteCombo(ComboMaintEndHour, ComboMaintEndMinute);

        LoadCurrentSettings();
    }

    private void FillHourMinuteCombo(ComboBox hourCombo, ComboBox minuteCombo)
    {
        for (int h = 0; h < 24; h++) hourCombo.Items.Add(h.ToString("D2"));
        for (int m = 0; m < 60; m++) minuteCombo.Items.Add(m.ToString("D2"));
    }

    private void LoadCurrentSettings()
    {
        ComboRestartHour.SelectedIndex = _server.Config.RestartHour;
        ComboRestartMinute.SelectedIndex = _server.Config.RestartMinute;
        ChkRestartEnabled.IsChecked = _server.Config.RestartScheduleEnabled;

        ComboMaintStartHour.SelectedIndex = _server.Config.MaintenanceStartHour;
        ComboMaintStartMinute.SelectedIndex = _server.Config.MaintenanceStartMinute;
        ComboMaintEndHour.SelectedIndex = _server.Config.MaintenanceEndHour;
        ComboMaintEndMinute.SelectedIndex = _server.Config.MaintenanceEndMinute;
        ChkMaintenanceEnabled.IsChecked = _server.Config.MaintenanceScheduleEnabled;
    }

    private void BtnApplyRestart_Click(object sender, RoutedEventArgs e)
    {
        _server.Config.RestartHour = ComboRestartHour.SelectedIndex;
        _server.Config.RestartMinute = ComboRestartMinute.SelectedIndex;
        _server.Config.RestartScheduleEnabled = ChkRestartEnabled.IsChecked == true;
        _server.SaveConfig();

        if (_server.Config.RestartScheduleEnabled)
        {
            _server.StartRestartSchedule();
            MessageBox.Show(Lang.Get("scheduled_applied"), "MCServerManager++");
        }
        else
        {
            _server.StopRestartSchedule();
            MessageBox.Show(Lang.Get("scheduled_disabled"), "MCServerManager++");
        }
    }

    private void BtnApplyMaintenance_Click(object sender, RoutedEventArgs e)
    {
        _server.Config.MaintenanceStartHour = ComboMaintStartHour.SelectedIndex;
        _server.Config.MaintenanceStartMinute = ComboMaintStartMinute.SelectedIndex;
        _server.Config.MaintenanceEndHour = ComboMaintEndHour.SelectedIndex;
        _server.Config.MaintenanceEndMinute = ComboMaintEndMinute.SelectedIndex;
        _server.Config.MaintenanceScheduleEnabled = ChkMaintenanceEnabled.IsChecked == true;
        _server.SaveConfig();

        if (_server.Config.MaintenanceScheduleEnabled)
        {
            _server.StartMaintenanceSchedule();
            MessageBox.Show(Lang.Get("scheduled_applied"), "MCServerManager++");
        }
        else
        {
            _server.StopMaintenanceSchedule();
            MessageBox.Show(Lang.Get("scheduled_disabled"), "MCServerManager++");
        }
    }
}