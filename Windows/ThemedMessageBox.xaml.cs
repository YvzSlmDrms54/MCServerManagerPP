using System.Windows;

namespace MCServerManagerPP;

public enum ThemedMessageBoxButtons
{
    Ok,
    OkCancel
}

public partial class ThemedMessageBox : Window
{
    public bool Result { get; private set; } = false;

    private ThemedMessageBox(string message, string title, ThemedMessageBoxButtons buttons)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;

        if (buttons == ThemedMessageBoxButtons.OkCancel)
        {
            BtnCancel.Visibility = Visibility.Visible;
            BtnCancel.Content = Lang.Get("themed_msgbox_cancel");
        }

        BtnOk.Content = Lang.Get("themed_msgbox_ok");
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        DialogResult = false;
        Close();
    }

    public static bool Show(string message, string title = "MCServerManager++", 
        ThemedMessageBoxButtons buttons = ThemedMessageBoxButtons.Ok, Window? owner = null)
    {
        var box = new ThemedMessageBox(message, title, buttons);
        if (owner != null) box.Owner = owner;
        bool? result = box.ShowDialog();
        return box.Result;
    }
}