using System.Collections.Generic;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MCServerManagerPP;

public partial class WebhookWindow : Window
{
    private readonly ServerManager _server;
    private List<WebhookEntry> _webhooks = new();

    public WebhookWindow(ServerManager server)
    {
        InitializeComponent();
        _server = server;

        Title = Lang.Get("webhook_title");
        LblAddNew.Text = Lang.Get("webhook_add_new");
        LblName.Text = Lang.Get("webhook_name");
        LblUrl.Text = Lang.Get("webhook_url");
        LblMention.Text = Lang.Get("webhook_mention");
        ChkStart.Content = Lang.Get("webhook_event_start");
        ChkStop.Content = Lang.Get("webhook_event_stop");
        ChkJoin.Content = Lang.Get("webhook_event_join");
        ChkLeave.Content = Lang.Get("webhook_event_leave");
        ChkBackup.Content = Lang.Get("webhook_event_backup");
        BtnAddWebhook.Content = Lang.Get("webhook_add_btn");
        LblRegistered.Text = Lang.Get("webhook_registered");
        BtnDeleteWebhook.Content = Lang.Get("webhook_delete");
        ChkMaintenance.Content = Lang.Get("webhook_event_maintenance");
        ChkCrash.Content = Lang.Get("webhook_event_crash");

        RefreshList();
    }

    private void RefreshList()
    {
        _webhooks = _server.LoadWebhooks();
        WebhookListBox.Items.Clear();
        foreach (var w in _webhooks)
        {
            WebhookListBox.Items.Add($"{w.Name}  —  {w.Url}");
        }
    }

    private void BtnAddWebhook_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtWebhookName.Text) || string.IsNullOrWhiteSpace(TxtWebhookUrl.Text))
        {
            ThemedMessageBox.Show(Lang.Get("webhook_empty_error"), "MCServerManager++", ThemedMessageBoxButtons.Ok, this);
            return;
        }

        var newWebhook = new WebhookEntry
        {
            Name = TxtWebhookName.Text.Trim(),
            Url = TxtWebhookUrl.Text.Trim(),
            MentionTarget = TxtMention.Text.Trim(),
            NotifyServerStart = ChkStart.IsChecked == true,
            NotifyServerStop = ChkStop.IsChecked == true,
            NotifyPlayerJoin = ChkJoin.IsChecked == true,
            NotifyPlayerLeave = ChkLeave.IsChecked == true,
            NotifyBackup = ChkBackup.IsChecked == true,
            NotifyMaintenance = ChkMaintenance.IsChecked == true,
            NotifyCrash = ChkCrash.IsChecked == true,
            
        };

        _webhooks.Add(newWebhook);
        _server.SaveWebhooksToConfig(_webhooks);

        TxtWebhookName.Clear();
        TxtWebhookUrl.Clear();
        ChkStart.IsChecked = false;
        ChkStop.IsChecked = false;
        ChkJoin.IsChecked = false;
        ChkLeave.IsChecked = false;
        ChkBackup.IsChecked = false;
        ChkMaintenance.IsChecked = false;
        ChkCrash.IsChecked = false;

        RefreshList();
    }

    private void BtnDeleteWebhook_Click(object sender, RoutedEventArgs e)
    {
        int index = WebhookListBox.SelectedIndex;
        if (index < 0)
        {
            ThemedMessageBox.Show(Lang.Get("webhook_select_delete"), "MCServerManager++", ThemedMessageBoxButtons.Ok, this);
            return;
        }

        _webhooks.RemoveAt(index);
        _server.SaveWebhooksToConfig(_webhooks);
        RefreshList();
    }
}