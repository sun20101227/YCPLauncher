using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

using UserControl = System.Windows.Controls.UserControl;

namespace YCPLauncher.Views;

public partial class ToastNotification : UserControl
{
    public ToastNotification()
    {
        InitializeComponent();
    }

    public void Show(string message, bool isError = false)
    {
        ToastText.Text = message;
        ToastIcon.Text = isError ? "⚠" : "✓";
        ToastBorder.BorderBrush = isError
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x00));
        ToastIcon.Foreground = ToastBorder.BorderBrush;

        var storyboard = new Storyboard();

        // Fade in
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        Storyboard.SetTarget(fadeIn, ToastBorder);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
        storyboard.Children.Add(fadeIn);

        // Hold at opacity=1 for 1.8s then fade out
        var hold = new DoubleAnimation(1, 1, TimeSpan.FromMilliseconds(1800))
        { BeginTime = TimeSpan.FromMilliseconds(200) };
        Storyboard.SetTarget(hold, ToastBorder);
        Storyboard.SetTargetProperty(hold, new PropertyPath("Opacity"));
        storyboard.Children.Add(hold);

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400))
        { BeginTime = TimeSpan.FromMilliseconds(2000) };
        Storyboard.SetTarget(fadeOut, ToastBorder);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
        storyboard.Children.Add(fadeOut);

        storyboard.Begin();
    }
}
