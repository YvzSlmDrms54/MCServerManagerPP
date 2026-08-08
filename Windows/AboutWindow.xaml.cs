using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MCServerManagerPP;

public partial class AboutWindow : Window
{
    private const string GithubUrl = "https://github.com/MyBetaSoft/MCServerManagerPP";
    private const string AppVersion = "1.0.0";

    public AboutWindow()
    {
        InitializeComponent();

        Title = Lang.Get("about_title");
        LblVersion.Text = $"{Lang.Get("about_version")}: {AppVersion}";
        LblDescription.Text = Lang.Get("about_description");
        LblDevelopedBy.Text = Lang.Get("about_developed_by");
        LnkGithub.Text = Lang.Get("about_github");
    }

    private void LnkGithub_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = GithubUrl,
            UseShellExecute = true
        });
    }
}