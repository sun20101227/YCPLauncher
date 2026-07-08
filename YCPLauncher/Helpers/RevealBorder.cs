using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YCPLauncher.Helpers;

public class RevealBorder : Border
{
    private RadialGradientBrush _hoverBrush;

    public static readonly DependencyProperty HoverColorProperty =
        DependencyProperty.Register("HoverColor", typeof(System.Windows.Media.Color), typeof(RevealBorder), new PropertyMetadata(System.Windows.Media.Color.FromArgb(80, 255, 255, 255), OnHoverColorChanged));

    public System.Windows.Media.Color HoverColor
    {
        get => (System.Windows.Media.Color)GetValue(HoverColorProperty);
        set => SetValue(HoverColorProperty, value);
    }

    public static readonly DependencyProperty HoverRadiusProperty =
        DependencyProperty.Register("HoverRadius", typeof(double), typeof(RevealBorder), new PropertyMetadata(200.0, OnHoverRadiusChanged));

    public double HoverRadius
    {
        get => (double)GetValue(HoverRadiusProperty);
        set => SetValue(HoverRadiusProperty, value);
    }

    public RevealBorder()
    {
        _hoverBrush = new RadialGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            RadiusX = HoverRadius,
            RadiusY = HoverRadius,
            GradientStops = new GradientStopCollection
            {
                new GradientStop(HoverColor, 0.0),
                new GradientStop(Colors.Transparent, 1.0)
            },
            Opacity = 0.0
        };

        this.BorderBrush = _hoverBrush;
        this.MouseMove += OnMouseMove;
        this.MouseEnter += OnMouseEnter;
        this.MouseLeave += OnMouseLeave;
    }

    private static void OnHoverColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RevealBorder rb)
        {
            rb._hoverBrush.GradientStops[0].Color = (System.Windows.Media.Color)e.NewValue;
        }
    }

    private static void OnHoverRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RevealBorder rb)
        {
            rb._hoverBrush.RadiusX = (double)e.NewValue;
            rb._hoverBrush.RadiusY = (double)e.NewValue;
        }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (IsMouseOver)
        {
            System.Windows.Point mousePos = e.GetPosition(this);
            _hoverBrush.Center = mousePos;
            _hoverBrush.GradientOrigin = mousePos;
        }
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (Services.ConfigService.GetConfig().EnableFluentGlass)
        {
            _hoverBrush.Opacity = 1.0;
        }
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _hoverBrush.Opacity = 0.0;
    }
}
