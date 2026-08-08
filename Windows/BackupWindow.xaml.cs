using System;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace MCServerManagerPP;

public partial class BackupWindow : Window
{
    private readonly ServerManager _server;
    private readonly DispatcherTimer _refreshTimer;

    public BackupWindow(ServerManager server)
    {
        InitializeComponent();
        _server = server;

        Title = Lang.Get("backup_title");
        LblAutoBackupHour.Text = Lang.Get("backup_auto_hour");
        ChkBackupEnabled.Content = Lang.Get("backup_active");
        BtnApply.Content = Lang.Get("backup_apply");
        BtnBackupNow.Content = Lang.Get("backup_now");
        LblBackupHistory.Text = Lang.Get("backup_history");

        for (int h = 0; h < 24; h++)
            ComboBackupHour.Items.Add(h.ToString("D2"));
        for (int m = 0; m < 60; m++)
            ComboBackupMinute.Items.Add(m.ToString("D2"));

        ComboBackupHour.SelectedIndex = _server.Config.Backup.Hour;
        ComboBackupMinute.SelectedIndex = _server.Config.Backup.Minute;
        ChkBackupEnabled.IsChecked = _server.Config.Backup.Enabled;

        RefreshLastBackupText();
        RefreshBackupList();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _refreshTimer.Tick += (s, e) => RefreshLastBackupText();
        _refreshTimer.Start();

        Closed += (s, e) => _refreshTimer.Stop();
    }

    private void RefreshLastBackupText()
    {
        if (_server.LastBackupTime == null)
        {
            LastBackupText.Text = $"{Lang.Get("backup_last_time")}: {Lang.Get("backup_never")}";
            return;
        }

        var time = _server.LastBackupTime.Value;
        var diff = DateTime.Now - time;
        string agoText = diff.TotalMinutes < 1
            ? Lang.Get("backup_ago_now")
            : $"{(int)diff.TotalMinutes} {Lang.Get("backup_ago_minutes")}";

        LastBackupText.Text = $"{Lang.Get("backup_last_time")}: {time:HH:mm} ({agoText})";
    }

    private void RefreshBackupList()
    {
        BackupListBox.Items.Clear();
        foreach (var backup in _server.GetBackupList())
        {
            BackupListBox.Items.Add($"{backup.FileName}  —  {backup.Date:dd.MM.yyyy HH:mm}");
        }
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        int hour = ComboBackupHour.SelectedIndex;
        int minute = ComboBackupMinute.SelectedIndex;
        bool enabled = ChkBackupEnabled.IsChecked == true;

        _server.Config.Backup.Hour = hour;
        _server.Config.Backup.Minute = minute;
        _server.Config.Backup.Enabled = enabled;
        _server.SaveConfig();

        if (enabled)
        {
            _server.StartAutoBackup(new TimeSpan(hour, minute, 0));
            MessageBox.Show(string.Format(Lang.Get("backup_applied"), $"{hour:D2}:{minute:D2}"), "MCServerManager++");
        }
        else
        {
            _server.StopAutoBackup();
            MessageBox.Show(Lang.Get("backup_disabled"), "MCServerManager++");
        }
    }

    private void BtnBackupNow_Click(object sender, RoutedEventArgs e)
    {
        string result = _server.BackupWorld();
        MessageBox.Show(result, "Yedekleme");
        RefreshLastBackupText();
        RefreshBackupList();
    }
}