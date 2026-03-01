using System.Windows;
using PropertyMatterHub.App.Services;
using PropertyMatterHub.App.ViewModels;

namespace PropertyMatterHub.App.Views;

public partial class FirstRunWindow : Window
{
    private readonly FirstRunService _firstRunService;

    public FirstRunWindow(SettingsViewModel vm, FirstRunService firstRunService)
    {
        InitializeComponent();
        DataContext      = vm;
        _firstRunService = firstRunService;
    }

    private async void ConnectGoogle_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var dialog = new GoogleCredentialsWindow { Owner = this };
        if (dialog.ShowDialog() != true) return;

        await vm.SetAndConnectAsync(dialog.ClientId!, dialog.ClientSecret!);
    }

    private async void GetStarted_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await _firstRunService.SaveUserSettingsAsync(new UserSettings(
                ZDriveRoot:        vm.ZDriveRoot,
                CaseFolderPattern: vm.CaseFolderPattern,
                CaseFolderDepth:   vm.CaseFolderDepth,
                ExcelPath:         vm.ExcelPath,
                DatabasePath:      @"Z:\PropertyMatterHub\hub.db"));
        }

        DialogResult = true;
        Close();
    }
}
