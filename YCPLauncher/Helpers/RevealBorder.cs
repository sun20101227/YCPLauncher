using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YCPLauncher.Helpers;

public class RevealBorder : Border
{
    private RadialGradientBrush _hoverBrush;
    private RadialGradientBrush _bgHoverBrush;

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

        _bgHoverBrush = new RadialGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            RadiusX = HoverRadius,
            RadiusY = HoverRadius,
            GradientStops = new GradientStopCollection
            {
                new GradientStop(System.Windows.Media.Color.FromArgb((byte)(HoverColor.A / 2), HoverColor.R, HoverColor.G, HoverColor.B), 0.0),
                new GradientStop(Colors.Transparent, 1.0)
            },
            Opacity = 0.0
        };

        this.MouseMove += OnMouseMove;
        this.MouseEnter += OnMouseEnter;
        this.MouseLeave += OnMouseLeave;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc); // Draws standard Background and static BorderBrush if any

        // Draw background hover glow
        if (_bgHoverBrush.Opacity > 0 && CornerRadius.TopLeft > 0)
        {
            dc.DrawRoundedRectangle(_bgHoverBrush, null, new Rect(0, 0, ActualWidth, ActualHeight), CornerRadius.TopLeft, CornerRadius.TopLeft);
        }
        else if (_bgHoverBrush.Opacity > 0)
        {
            dc.DrawRectangle(_bgHoverBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));
        }

        // Draw border hover glow on top
        if (_hoverBrush.Opacity > 0 && BorderThickness.Top > 0)
        {
            var hoverPen = new System.Windows.Media.Pen(_hoverBrush, BorderThickness.Top);
            double halfThickness = hoverPen.Thickness / 2.0;
            Rect rect = new Rect(halfThickness, halfThickness, ActualWidth - hoverPen.Thickness, ActualHeight - hoverPen.Thickness);
            
            if (CornerRadius.TopLeft > 0)
                dc.DrawRoundedRectangle(null, hoverPen, rect, CornerRadius.TopLeft, CornerRadius.TopLeft);
            else
                dc.DrawRectangle(null, hoverPen, rect);
        }
    }

    private static void OnHoverColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RevealBorder rb)
        {
            var newColor = (System.Windows.Media.Color)e.NewValue;
            rb._hoverBrush.GradientStops[0].Color = newColor;
            rb._bgHoverBrush.GradientStops[0].Color = System.Windows.Media.Color.FromArgb((byte)(newColor.A / 2), newColor.R, newColor.G, newColor.B);
        }
    }

    private static void OnHoverRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RevealBorder rb)
        {
            rb._hoverBrush.RadiusX = (double)e.NewValue;
            rb._hoverBrush.RadiusY = (double)e.NewValue;
            rb._bgHoverBrush.RadiusX = (double)e.NewValue;
            rb._bgHoverBrush.RadiusY = (double)e.NewValue;
        }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (IsMouseOver)
        {
            System.Windows.Point mousePos = e.GetPosition(this);
            _hoverBrush.Center = mousePos;
            _hoverBrush.GradientOrigin = mousePos;
            _bgHoverBrush.Center = mousePos;
            _bgHoverBrush.GradientOrigin = mousePos;
            InvalidateVisual();
        }
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!Services.ConfigService.GetConfig().ReduceAnimations)
        {
            _hoverBrush.Opacity = 1.0;
            _bgHoverBrush.Opacity = 1.0;
        }
        InvalidateVisual();
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _hoverBrush.Opacity = 0.0;
        _bgHoverBrush.Opacity = 0.0;
        InvalidateVisual();
    }
}
