using System.Windows;

namespace MCServerManagerPP;

public partial class PlayerManagementWindow : Window
{
    private readonly ServerManager _server;

    public PlayerManagementWindow(ServerManager server)
    {
        InitializeComponent();
        _server = server;

        Title = Lang.Get("players_mgmt_title");
        TabOp.Header = Lang.Get("players_tab_op");
        TabWhitelist.Header = Lang.Get("players_tab_whitelist");
        TabBan.Header = Lang.Get("players_tab_ban");

        BtnOpAdd.Content = Lang.Get("players_op_add");
        BtnOpRemove.Content = Lang.Get("players_op_remove");
        BtnOpRefresh.Content = Lang.Get("players_refresh");
        LblOpCurrent.Text = Lang.Get("players_current_list");

        BtnWhitelistAdd.Content = Lang.Get("players_whitelist_add");
        BtnWhitelistRemove.Content = Lang.Get("players_whitelist_remove");
        BtnWhitelistRefresh.Content = Lang.Get("players_refresh");
        LblWhitelistCurrent.Text = Lang.Get("players_current_list");

        BtnBanAdd.Content = Lang.Get("players_ban_add");
        BtnBanRemove.Content = Lang.Get("players_ban_remove");
        BtnBanRefresh.Content = Lang.Get("players_refresh");
        LblBanCurrent.Text = Lang.Get("players_current_list");

        RefreshOpList();
        RefreshWhitelistList();
        RefreshBanList();
    }

    private void RefreshOpList()
    {
        OpListBox.ItemsSource = null;
        OpListBox.ItemsSource = _server.GetOpsList();
    }

    private void RefreshWhitelistList()
    {
        WhitelistListBox.ItemsSource = null;
        WhitelistListBox.ItemsSource = _server.GetWhitelistList();
    }

    private void RefreshBanList()
    {
        BanListBox.ItemsSource = null;
        BanListBox.ItemsSource = _server.GetBannedList();
    }

    private void BtnOpAdd_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtOpName.Text)) return;
        _server.OpPlayer(TxtOpName.Text.Trim());
        TxtOpName.Clear();
    }

    private void BtnOpRemove_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtOpName.Text)) return;
        _server.DeopPlayer(TxtOpName.Text.Trim());
        TxtOpName.Clear();
    }

    private void BtnOpRefresh_Click(object sender, RoutedEventArgs e) => RefreshOpList();

    private void BtnWhitelistAdd_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtWhitelistName.Text)) return;
        _server.WhitelistAdd(TxtWhitelistName.Text.Trim());
        TxtWhitelistName.Clear();
    }

    private void BtnWhitelistRemove_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtWhitelistName.Text)) return;
        _server.WhitelistRemove(TxtWhitelistName.Text.Trim());
        TxtWhitelistName.Clear();
    }

    private void BtnWhitelistRefresh_Click(object sender, RoutedEventArgs e) => RefreshWhitelistList();

    private void BtnBanAdd_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtBanName.Text)) return;
        _server.BanPlayer(TxtBanName.Text.Trim());
        TxtBanName.Clear();
    }

    private void BtnBanRemove_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtBanName.Text)) return;
        _server.PardonPlayer(TxtBanName.Text.Trim());
        TxtBanName.Clear();
    }

    private void BtnBanRefresh_Click(object sender, RoutedEventArgs e) => RefreshBanList();
}