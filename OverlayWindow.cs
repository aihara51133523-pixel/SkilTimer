using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BuffTimer;
internal sealed class OverlayWindow : Window
{
    private readonly OutlinedNumber number = new();
    public OverlayWindow()
    {
        Title = "Buff Timer — 表示位置";
        Width = 230; Height = 110;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent;
        Topmost = true; ShowInTaskbar = false; ShowActivated = false;
        Content = number;
        Left = SystemParameters.WorkArea.Right - Width;
        Top = SystemParameters.WorkArea.Top;
        MouseLeftButtonDown += (_, e) => { Activate(); DragMove(); e.Handled = true; };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) Owner?.Activate(); };
    }
    public void Display(double seconds, bool expired)
    {
        string value = seconds.ToString("F2", CultureInfo.InvariantCulture);
        if (number.Value == value && number.Expired == expired) return;
        number.Value = value; number.Expired = expired; number.InvalidateVisual();
    }
    private sealed class OutlinedNumber : FrameworkElement
    {
        public string Value = "30.00";
        public bool Expired;
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
            var text = new FormattedText(Value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                72, Brushes.LightBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            if (text.Width > ActualWidth - 12)
                text.SetFontSize(72 * (ActualWidth - 12) / text.Width);
            var shape = text.BuildGeometry(new Point((ActualWidth - text.Width) / 2, (ActualHeight - text.Height) / 2));
            dc.DrawGeometry(Brushes.LightBlue, new Pen(Expired ? Brushes.Red : Brushes.Black, 5) { LineJoin = PenLineJoin.Round }, shape);
            dc.DrawGeometry(Brushes.LightBlue, null, shape);
        }
    }
}
