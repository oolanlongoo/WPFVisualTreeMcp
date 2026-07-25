using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfVisualTreeMcp.Inspector;

/// <summary>
/// Highlights UI elements with a visual overlay.
/// </summary>
public class ElementHighlighter
{
    private Window? _overlayWindow;
    private DispatcherTimer? _hideTimer;

    /// <summary>
    /// Shows a red overlay over <paramref name="element"/> for <paramref name="durationMs"/>.
    /// Returns false when the element has no valid on-screen bounds (no window, zero size, etc.).
    /// </summary>
    public bool Highlight(UIElement element, int durationMs = 2000)
    {
        var bounds = GetElementScreenBoundsInDips(element);
        if (bounds == Rect.Empty || bounds.Width <= 0 || bounds.Height <= 0)
            return false;

        // Remove existing overlay
        HideOverlay();

        // Create highlight overlay
        _overlayWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            Topmost = true,
            ShowInTaskbar = false,
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            IsHitTestVisible = false
        };

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
            BorderThickness = new Thickness(3),
            Background = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
        };

        _overlayWindow.Content = border;
        _overlayWindow.Show();

        // Set up timer to hide
        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(durationMs)
        };
        _hideTimer.Tick += (s, e) =>
        {
            HideOverlay();
        };
        _hideTimer.Start();
        return true;
    }

    private void HideOverlay()
    {
        _hideTimer?.Stop();
        _hideTimer = null;

        _overlayWindow?.Close();
        _overlayWindow = null;
    }

    /// <summary>
    /// Device-pixel screen bounds via PointToScreen, converted to DIPs for WPF Window placement.
    /// </summary>
    private static Rect GetElementScreenBoundsInDips(UIElement element)
    {
        try
        {
            var size = element.RenderSize;
            if (size.Width <= 0 || size.Height <= 0)
                return Rect.Empty;

            var topLeftDevice = element.PointToScreen(new Point(0, 0));
            var bottomRightDevice = element.PointToScreen(new Point(size.Width, size.Height));

            var source = PresentationSource.FromVisual(element);
            var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var topLeft = fromDevice.Transform(topLeftDevice);
            var bottomRight = fromDevice.Transform(bottomRightDevice);

            return new Rect(topLeft, bottomRight);
        }
        catch
        {
            return Rect.Empty;
        }
    }
}
