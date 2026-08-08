using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using System.Drawing;
using System.Windows.Forms;

namespace MCServerManagerPP;

public partial class MainWindow : Window
{
    private readonly ServerManager _server = new();
    private bool _isClosingForReal = false;
    private NotifyIcon? _trayIcon;
    private List<int> _searchMatches = new();
    private int _currentMatchIndex = -1;

    public MainWindow()
    {
        InitializeComponent();
        _server.OnLogReceived += AppendLog; 
        _server.OnServerStopped += () => Dispatcher.Invoke(() =>
        {
            StatusText.Text = Lang.Get("status_closed");
            UpdateButtonStates();
        });
        _server.OnPlayerListUpdated += UpdatePlayerList;

        ApplyLanguage();
        UpdateButtonStates();
        InitTrayIcon();
        

        Closing += (s, e) =>
        {
            if (_isClosingForReal) return;

            e.Cancel = true;
            Hide();
        };

        Closed += (s, e) => Application.Current.Shutdown();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (SearchBar.Visibility == Visibility.Visible)
            {
                SearchBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                SearchBar.Visibility = Visibility.Visible;
                TxtSearch.Focus();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SearchBar.Visibility = Visibility.Collapsed;
        }
    }

    private void AppendLog(string line)
    {
        Dispatcher.Invoke(() =>
        {
            ConsoleOutput.AppendText(line + "\n");
            ConsoleOutput.ScrollToEnd();
        });
    }

    private void ApplyLanguage()
    {
        BtnStart.Content = Lang.Get("btn_start");
        BtnStop.Content = Lang.Get("btn_stop");
        BtnRestart.Content = Lang.Get("btn_restart");
        BtnFastBackup.Content = Lang.Get("btn_fast_backup");
        BtnSend.Content = Lang.Get("send");
        PlayersHeader.Text = Lang.Get("players_header");
        StatusText.Text = Lang.Get("status_closed");
        BtnSearchPrev.Content = Lang.Get("search_prev");
        BtnSearchNext.Content = Lang.Get("search_next");

        MenuServer.Header = Lang.Get("menu_server");
        MenuStart.Header = Lang.Get("btn_start");
        MenuStop.Header = Lang.Get("btn_stop");
        MenuRestart.Header = Lang.Get("btn_restart");

        MenuTools.Header = Lang.Get("menu_tools");
        MenuProperties.Header = Lang.Get("menu_properties");
        MenuBackup.Header = Lang.Get("menu_backup");
        MenuWebhooks.Header = Lang.Get("menu_webhooks");
        MenuPlayers.Header = Lang.Get("menu_players");

        MenuHelp.Header = Lang.Get("menu_help");
        MenuAbout.Header = Lang.Get("menu_about");
        MenuKeybinds.Header = Lang.Get("menu_keybinds");

        MenuMaintenance.Header = Lang.Get("menu_maintenance");
        MenuScheduled.Header = Lang.Get("menu_scheduled");

        UpdateServerTypeDisplay();
    }

    private void InitTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon("icon.ico"),
            Visible = true,
            Text = "MCServerManager++"
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(Lang.Get("tray_show"), null, (s, e) => ShowFromTray());
        menu.Items.Add(Lang.Get("tray_stop_server"), null, (s, e) =>
        {
            if (_server.IsRunning) _server.StopServer();
        });
        menu.Items.Add(Lang.Get("tray_exit"), null, (s, e) =>
        {
            _isClosingForReal = true;
            _trayIcon!.Visible = false;
            Close();
        });

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (s, e) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void UpdateServerTypeDisplay()
    {
        string type = _server.DetectServerType();
        ServerTypeText.Text = type switch
        {
            "vanilla" => Lang.Get("server_type_vanilla"),
            "paper" => Lang.Get("server_type_paper"),
            "fabric" => Lang.Get("server_type_fabric"),
            "forge" => Lang.Get("server_type_forge"),
            "fabric_or_forge" => Lang.Get("server_type_modded"),
            _ => Lang.Get("server_type_not_installed")
        };
    }   

    private void UpdateButtonStates()
    {
        BtnStart.IsEnabled = !_server.IsRunning;
        BtnStop.IsEnabled = _server.IsRunning;
    }

    private void UpdatePlayerList(List<string> players)
    {
        Dispatcher.Invoke(() =>
        {
            PlayerList.ItemsSource = players;
        });
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (!_server.IsEulaAccepted())
        {
            var result = MessageBox.Show(
                "Do you agree to the Minecraft EULA? (https://www.minecraft.net/en-us/eula)",
                "Minecraft EULA",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                _server.AcceptEula();
            }
            else
            {
                return;
            }
        }

        _server.StartServer();
        _server.StartPlayerListPolling();
        StatusText.Text = Lang.Get("status_open");
        UpdateButtonStates();
    }

    private async void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        BtnStop.IsEnabled = false;
        _server.StopServer();

        while (_server.IsRunning)
        {
            await System.Threading.Tasks.Task.Delay(300);
        }

        _server.StopPlayerListPolling();
        _server.StopAutoBackup();
        UpdateButtonStates();
    }

    private async void BtnRestart_Click(object sender, RoutedEventArgs e)
    {
        if (!_server.IsRunning) return;

        BtnRestart.IsEnabled = false;
        _server.StopServer();

        while (_server.IsRunning)
        {
            await System.Threading.Tasks.Task.Delay(300);
        }

        _server.StartServer();
        UpdateButtonStates();
        BtnRestart.IsEnabled = true;
    }

    private void BtnFastBackup_Click(object sender, RoutedEventArgs e)
    {
        string result = _server.BackupWorld();
        MessageBox.Show(result, "MCServerManager++");
    }

    private void BtnProperties_Click(object sender, RoutedEventArgs e)
    {
        var propsWindow = new PropertiesWindow(_server) { Owner = this };
        propsWindow.ShowDialog();
    }

    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        var backupWindow = new BackupWindow(_server) { Owner = this };
        backupWindow.ShowDialog();
    }

    private void BtnWebhooks_Click(object sender, RoutedEventArgs e)
    {
        var webhookWindow = new WebhookWindow(_server) { Owner = this };
        webhookWindow.ShowDialog();
    }

    private void BtnPlayerMgmt_Click(object sender, RoutedEventArgs e)
    {
        var pmWindow = new PlayerManagementWindow(_server) { Owner = this };
        pmWindow.ShowDialog();
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow { Owner = this };
        aboutWindow.ShowDialog();
    }

    private void MenuKeybinds_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(Lang.Get("keybinds_list"), Lang.Get("menu_keybinds"));
    }

    private void BtnMaintenance_Click(object sender, RoutedEventArgs e)
    {
        var maintWindow = new MaintenanceWindow(_server) { Owner = this };
        maintWindow.ShowDialog();
    }

    private void BtnScheduled_Click(object sender, RoutedEventArgs e)
    {
        var schedWindow = new ScheduledTasksWindow(_server) { Owner = this };
        schedWindow.ShowDialog();
    }

    private void BtnSend_Click(object sender, RoutedEventArgs e)
    {
        SendCurrentCommand();
    }

    private void CommandInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SendCurrentCommand();
    }

    private void SendCurrentCommand()
    {
        if (string.IsNullOrWhiteSpace(CommandInput.Text)) return;
        _server.SendCommand(CommandInput.Text);
        CommandInput.Clear();
    }

    private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PerformSearch();
        }
    }

    private void BtnSearchNext_Click(object sender, RoutedEventArgs e)
    {
        if (_searchMatches.Count == 0) { PerformSearch(); return; }
        _currentMatchIndex = (_currentMatchIndex + 1) % _searchMatches.Count;
        JumpToMatch();
    }

    private void BtnSearchPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_searchMatches.Count == 0) { PerformSearch(); return; }
        _currentMatchIndex = (_currentMatchIndex - 1 + _searchMatches.Count) % _searchMatches.Count;
        JumpToMatch();
    }

    private void PerformSearch()
    {
        _searchMatches.Clear();
        _currentMatchIndex = -1;

        string query = TxtSearch.Text;
        if (string.IsNullOrWhiteSpace(query))
        {
            SearchStatusText.Text = "";
            return;
        }

        string text = ConsoleOutput.Text;
        int index = 0;
        while ((index = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            _searchMatches.Add(index);
            index += query.Length;
        }

        if (_searchMatches.Count == 0)
        {
            SearchStatusText.Text = Lang.Get("search_no_match");
            return;
        }

        _currentMatchIndex = 0;
        JumpToMatch();
    }

    private void JumpToMatch()
    {
        if (_currentMatchIndex < 0 || _currentMatchIndex >= _searchMatches.Count) return;

        int pos = _searchMatches[_currentMatchIndex];
        int length = TxtSearch.Text.Length;

        ConsoleOutput.Focus();
        ConsoleOutput.Select(pos, length);
        ConsoleOutput.ScrollToLine(ConsoleOutput.GetLineIndexFromCharacterIndex(pos));

        SearchStatusText.Text = $"{_currentMatchIndex + 1}/{_searchMatches.Count}";
    }
}