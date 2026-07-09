using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace YCPLauncher.Helpers;

public static class SmoothScrollViewerBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SmoothScrollViewerBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            if ((bool)e.NewValue)
            {
                scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            }
            else
            {
                scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
            }
        }
    }

    private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            e.Handled = true;
            
            // Calculate target offset
            double scrollFactor = 80.0;
            double targetOffset = scrollViewer.VerticalOffset - (e.Delta > 0 ? scrollFactor : -scrollFactor);
            
            // Clamp target offset
            if (targetOffset < 0) targetOffset = 0;
            if (targetOffset > scrollViewer.ScrollableHeight) targetOffset = scrollViewer.ScrollableHeight;
            
            // Animate scroll
            DoubleAnimation animation = new DoubleAnimation
            {
                From = scrollViewer.VerticalOffset,
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            scrollViewer.BeginAnimation(VerticalOffsetAnimationProperty, animation);
        }
    }
    
    public static readonly DependencyProperty VerticalOffsetAnimationProperty =
        DependencyProperty.RegisterAttached("VerticalOffsetAnimation", typeof(double), typeof(SmoothScrollViewerBehavior), new PropertyMetadata(0.0, OnVerticalOffsetAnimationChanged));
        
    private static void OnVerticalOffsetAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}
