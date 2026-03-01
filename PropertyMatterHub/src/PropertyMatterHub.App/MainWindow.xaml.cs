using PropertyMatterHub.App.ViewModels;
using Wpf.Ui.Controls;

namespace PropertyMatterHub.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // Wire search box input trigger
        SearchBox.PreviewKeyUp += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                vm.SearchCommand.Execute(null);
            else if (e.Key == System.Windows.Input.Key.Escape)
                vm.ClearSearchCommand.Execute(null);
        };

        // Wire search result navigation
        vm.Search.MatterSelected += id => vm.OpenMatter(id);
        vm.MatterList.MatterSelected += id => vm.OpenMatter(id);
    }
}
