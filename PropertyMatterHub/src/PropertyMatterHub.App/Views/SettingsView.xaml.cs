using System.Windows;
using System.Windows.Controls;
using PropertyMatterHub.App.ViewModels;

namespace PropertyMatterHub.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    /// <summary>
    /// Opens the credentials dialog then hands the result to the ViewModel.
    /// Hooked from XAML's Click event so no WPF types leak into the ViewModel.
    /// </summary>
    private async void ConnectGoogle_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var dialog = new GoogleCredentialsWindow { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;

        await vm.SetAndConnectAsync(dialog.ClientId!, dialog.ClientSecret!);
    }
}
