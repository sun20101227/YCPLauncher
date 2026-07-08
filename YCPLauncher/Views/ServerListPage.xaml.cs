using System.Windows.Controls;
using YCPLauncher.ViewModels;

namespace YCPLauncher.Views;

public partial class ServerListPage : Page
{
    public ServerListPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ServerListViewModel vm)
                await vm.LoadServersAsync();
        };
    }
}
