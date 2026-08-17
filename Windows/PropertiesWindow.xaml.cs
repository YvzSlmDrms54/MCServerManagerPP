using System.Windows;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;

namespace MCServerManagerPP;

public partial class PropertiesWindow : Window
{
    private readonly ServerManager _server;
    private System.Collections.Generic.Dictionary<string, string> _properties = new();

    public PropertiesWindow(ServerManager server)
    {
        InitializeComponent();
        _server = server;

        Title = Lang.Get("props_title");
        BtnSave.Content = Lang.Get("props_save");

        TabGeneral.Header = Lang.Get("props_tab_general");
        TabGamerules.Header = Lang.Get("props_tab_gamerules");
        TabPerformance.Header = Lang.Get("props_tab_performance");
        TabSecurity.Header = Lang.Get("props_tab_security");

        LblMotd.Text = Lang.Get("props_motd");
        LblMaxPlayers.Text = Lang.Get("props_max_players");
        LblOnlineMode.Text = Lang.Get("props_online_mode");
        LblWhitelist.Text = Lang.Get("props_whitelist");

        LblGamemode.Text = Lang.Get("props_gamemode");
        LblDifficulty.Text = Lang.Get("props_difficulty");
        LblPvp.Text = Lang.Get("props_pvp");
        LblHardcore.Text = Lang.Get("props_hardcore");
        LblSpawnMonsters.Text = Lang.Get("props_spawn_monsters");
        LblAllowFlight.Text = Lang.Get("props_allow_flight");
        LblForceGamemode.Text = Lang.Get("props_force_gamemode");

        LblViewDistance.Text = Lang.Get("props_view_distance");
        LblSimDistance.Text = Lang.Get("props_simulation_distance");
        LblSpawnProtection.Text = Lang.Get("props_spawn_protection");

        LblCommandBlock.Text = Lang.Get("props_enable_command_block");
        LblRequireResourcePack.Text = Lang.Get("props_require_resource_pack");

        LoadIntoUI();
    }

    private void LoadIntoUI()
    {
        _properties = _server.LoadProperties();

        TxtMotd.Text = GetValue("motd", "A Minecraft Server");
        TxtMaxPlayers.Text = GetValue("max-players", "20");
        TxtViewDistance.Text = GetValue("view-distance", "10");
        TxtSimDistance.Text = GetValue("simulation-distance", "10");
        TxtSpawnProtection.Text = GetValue("spawn-protection", "0");

        SetCombo(ComboGamemode, GetValue("gamemode", "survival"));
        SetCombo(ComboDifficulty, GetValue("difficulty", "easy"));

        ToggleWhitelist.IsChecked = GetValue("white-list", "false") == "true";
        ToggleOnlineMode.IsChecked = GetValue("online-mode", "true") == "true";
        ToggleAllowFlight.IsChecked = GetValue("allow-flight", "false") == "true";
        ToggleForceGamemode.IsChecked = GetValue("force-gamemode", "false") == "true";
        ToggleRequireResourcePack.IsChecked = GetValue("require-resource-pack", "false") == "true";
        TogglePvp.IsChecked = GetValue("pvp", "true") == "true";
        ToggleHardcore.IsChecked = GetValue("hardcore", "false") == "true";
        ToggleSpawnMonsters.IsChecked = GetValue("spawn-monsters", "true") == "true";
        ToggleCommandBlock.IsChecked = GetValue("enable-command-block", "false") == "true";
    }

    private string GetValue(string key, string fallback)
    {
        return _properties.TryGetValue(key, out var value) ? value : fallback;
    }

    private void SetCombo(ComboBox combo, string value)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if ((string)item.Content == value)
            {
                combo.SelectedItem = item;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private void BtnMaxPlayersUp_Click(object sender, RoutedEventArgs e) => StepNumber(TxtMaxPlayers, 1);
    private void BtnMaxPlayersDown_Click(object sender, RoutedEventArgs e) => StepNumber(TxtMaxPlayers, -1);
    private void BtnSpawnProtectionUp_Click(object sender, RoutedEventArgs e) => StepNumber(TxtSpawnProtection, 1);
    private void BtnSpawnProtectionDown_Click(object sender, RoutedEventArgs e) => StepNumber(TxtSpawnProtection, -1);
    private void BtnViewDistanceUp_Click(object sender, RoutedEventArgs e) => StepNumber(TxtViewDistance, 1);
    private void BtnViewDistanceDown_Click(object sender, RoutedEventArgs e) => StepNumber(TxtViewDistance, -1);
    private void BtnSimDistanceUp_Click(object sender, RoutedEventArgs e) => StepNumber(TxtSimDistance, 1);
    private void BtnSimDistanceDown_Click(object sender, RoutedEventArgs e) => StepNumber(TxtSimDistance, -1);

    private void StepNumber(TextBox box, int delta)
    {
        if (int.TryParse(box.Text, out int value))
        {
            value = System.Math.Max(0, value + delta);
            box.Text = value.ToString();
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _properties["motd"] = TxtMotd.Text;
        _properties["max-players"] = TxtMaxPlayers.Text;
        _properties["view-distance"] = TxtViewDistance.Text;
        _properties["simulation-distance"] = TxtSimDistance.Text;
        _properties["spawn-protection"] = TxtSpawnProtection.Text;
        _properties["gamemode"] = (string)((ComboBoxItem)ComboGamemode.SelectedItem).Content;
        _properties["difficulty"] = (string)((ComboBoxItem)ComboDifficulty.SelectedItem).Content;
        _properties["white-list"] = ToggleWhitelist.IsChecked == true ? "true" : "false";
        _properties["online-mode"] = ToggleOnlineMode.IsChecked == true ? "true" : "false";
        _properties["allow-flight"] = ToggleAllowFlight.IsChecked == true ? "true" : "false";
        _properties["force-gamemode"] = ToggleForceGamemode.IsChecked == true ? "true" : "false";
        _properties["require-resource-pack"] = ToggleRequireResourcePack.IsChecked == true ? "true" : "false";
        _properties["pvp"] = TogglePvp.IsChecked == true ? "true" : "false";
        _properties["hardcore"] = ToggleHardcore.IsChecked == true ? "true" : "false";
        _properties["spawn-monsters"] = ToggleSpawnMonsters.IsChecked == true ? "true" : "false";
        _properties["enable-command-block"] = ToggleCommandBlock.IsChecked == true ? "true" : "false";

        _server.SaveProperties(_properties);
        ThemedMessageBox.Show(Lang.Get("props_saved"), "MCServerManager++", ThemedMessageBoxButtons.Ok, this);
        Close();
    }
}