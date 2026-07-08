using System.Windows.Controls;
using YCPLauncher.ViewModels;

using UserControl = System.Windows.Controls.UserControl;

namespace YCPLauncher.Views;

public partial class ServerListView : UserControl
{
    public ServerListView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ServerListViewModel vm)
                await vm.LoadServersAsync();
        };
    }
}
