using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PropertyMatterHub.App.Views;

public partial class GoogleCredentialsWindow : Window
{
    public string? ClientId     { get; private set; }
    public string? ClientSecret { get; private set; }

    public GoogleCredentialsWindow()
    {
        InitializeComponent();
    }

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        var id     = ClientIdBox.Text.Trim();
        var secret = ClientSecretBox.Password.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(secret))
        {
            MessageBox.Show(
                "Please enter both a Client ID and a Client Secret.",
                "Missing credentials",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ClientId     = id;
        ClientSecret = secret;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
