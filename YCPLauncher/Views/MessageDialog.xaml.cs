using System.Windows;
using System.Windows.Input;

namespace YCPLauncher.Views;

public partial class MessageDialog : Window
{
    public MessageDialog(string message, string title = "提示")
    {
        InitializeComponent();
        TxtMessage.Text = message;
        TxtTitle.Text = title;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
