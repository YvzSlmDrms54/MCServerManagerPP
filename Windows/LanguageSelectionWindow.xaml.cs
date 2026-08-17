using System.Windows;
using System.Windows.Controls;

namespace MCServerManagerPP;

public partial class LanguageSelectWindow : Window
{
    public string SelectedLanguage { get; private set; } = "tr";

    public LanguageSelectWindow()
    {
        InitializeComponent();
        ComboLanguage.SelectedIndex = 0;
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        var item = (ComboBoxItem)ComboLanguage.SelectedItem;
        string content = (string)item.Content;
        SelectedLanguage = content == "English" ? "en" : "tr";
        DialogResult = true;
        Close();
    }
}