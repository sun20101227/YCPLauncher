using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace YCPLauncher.Helpers;

public static class SmoothScrollViewerBehavior
{
    private sealed class ScrollState
    {
        public double TargetOffset;
        public bool HasTarget;
    }

    private static readonly ConditionalWeakTable<ScrollViewer, ScrollState> ScrollStates = new();
    private static readonly CubicEase ScrollEase = new() { EasingMode = EasingMode.EaseOut };

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
                scrollViewer.BeginAnimation(VerticalOffsetAnimationProperty, null);
                ScrollStates.Remove(scrollViewer);
            }
        }
    }

    private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (!Services.ConfigService.AreAnimationsEnabled)
                return;

            e.Handled = true;

            var state = ScrollStates.GetOrCreateValue(scrollViewer);
            if (!state.HasTarget || Math.Abs(scrollViewer.VerticalOffset - state.TargetOffset) > 360)
            {
                state.TargetOffset = scrollViewer.VerticalOffset;
                state.HasTarget = true;
            }

            // Keep each wheel step restrained so content tracks the pointer rather than
            // queueing long layout animations that make the whole window feel sluggish.
            state.TargetOffset = Math.Clamp(
                state.TargetOffset - Math.Sign(e.Delta) * 76.0,
                0,
                scrollViewer.ScrollableHeight);

            scrollViewer.SetValue(VerticalOffsetAnimationProperty, scrollViewer.VerticalOffset);
            var animation = new DoubleAnimation
            {
                From = scrollViewer.VerticalOffset,
                To = state.TargetOffset,
                Duration = TimeSpan.FromMilliseconds(145),
                EasingFunction = ScrollEase,
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (_, _) =>
            {
                scrollViewer.BeginAnimation(VerticalOffsetAnimationProperty, null);
                scrollViewer.SetValue(VerticalOffsetAnimationProperty, state.TargetOffset);
                state.HasTarget = false;
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
