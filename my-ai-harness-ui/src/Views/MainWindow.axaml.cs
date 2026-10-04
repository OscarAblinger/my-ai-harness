using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using my_ai_harness_ui.ViewModels;

namespace my_ai_harness_ui.Views;

public partial class MainWindow : Window
{
    /// <summary>How close to the bottom (in px) counts as "pinned to the latest message".</summary>
    private const double PinnedThreshold = 48;

    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        Opened += (_, _) =>
        {
            ApplyDarkTitleBar();
            Composer.FocusEditor();
        };
    }

    /// <summary>
    /// The window chrome defaults to the system (light) colour, which clashes with the fixed dark
    /// theme - ask DWM for a dark title bar instead.
    /// </summary>
    private void ApplyDarkTitleBar()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
            return;

        // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 2004 and later), 19 before that.
        var useDarkMode = 1;
        if (DwmSetWindowAttribute(handle, 20, ref useDarkMode, sizeof(int)) != 0)
            DwmSetWindowAttribute(handle, 19, ref useDarkMode, sizeof(int));

        // DWM only repaints the caption with the new colour after a frame change, and Avalonia has
        // already painted it by the time the window opens. Toggling WS_CAPTION synchronously forces
        // that repaint without any visible flicker.
        const int GwlStyle = -16;
        const int WsCaption = 0x00C00000;
        const uint SwpNoSize = 0x0002;
        const uint SwpNoZOrder = 0x0001;
        const uint SwpNoActivate = 0x0010;
        const uint SwpFrameChanged = 0x0020;

        var style = GetWindowLong(handle, GwlStyle);

        SetWindowLong(handle, GwlStyle, style & ~WsCaption);
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);

        SetWindowLong(handle, GwlStyle, style);
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.TranscriptChanged -= OnTranscriptChanged;

        _viewModel = DataContext as MainViewModel;

        if (_viewModel is not null)
            _viewModel.TranscriptChanged += OnTranscriptChanged;
    }

    private void OnTranscriptChanged(object? sender, EventArgs e)
    {
        // Posted at Background priority so the new content has been measured first.
        Dispatcher.UIThread.Post(ScrollTranscriptToEndIfPinned, DispatcherPriority.Background);
    }

    /// <summary>
    /// Follows new content, but only while the reader is already at the bottom - scrolling up to
    /// read an earlier message must not be fought.
    /// </summary>
    private void ScrollTranscriptToEndIfPinned()
    {
        var scroller = TranscriptScroller;
        var endOffset = scroller.Extent.Height - scroller.Viewport.Height;
        if (endOffset <= 0)
            return;

        if (scroller.Offset.Y >= endOffset - PinnedThreshold)
            scroller.Offset = new Vector(0, endOffset);
    }
}
