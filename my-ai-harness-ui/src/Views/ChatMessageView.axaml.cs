using System;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Ablinger.MyAiHarness.UI.ViewModels;

namespace Ablinger.MyAiHarness.UI.Views;

public partial class ChatMessageView : UserControl
{
    private static readonly Geometry CopyGeometry = Geometry.Parse(
        "M11,9 H20 A2,2 0 0 1 22,11 V20 A2,2 0 0 1 20,22 H11 A2,2 0 0 1 9,20 V11 A2,2 0 0 1 11,9 Z " +
        "M5,15 H4 A2,2 0 0 1 2,13 V4 A2,2 0 0 1 4,2 H13 A2,2 0 0 1 15,4 V5");

    private static readonly Geometry CheckGeometry = Geometry.Parse("M20,6 L9,17 L4,12");

    private readonly DispatcherTimer _copyResetTimer;

    public ChatMessageView()
    {
        InitializeComponent();

        // One timer per message view: shows the checkmark feedback, then restores the glyph.
        _copyResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1600) };
        _copyResetTimer.Tick += OnCopyReset;
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ChatMessageViewModel viewModel)
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;

        await clipboard.SetTextAsync(viewModel.Markdown);

        // Toggled-on feedback: accent fill + checkmark for a moment.
        CopyButton.Classes.Add("copied");
        CopyGlyph.Data = CheckGeometry;
        _copyResetTimer.Stop();
        _copyResetTimer.Start();
    }

    private void OnCopyReset(object? sender, EventArgs e)
    {
        _copyResetTimer.Stop();
        CopyButton.Classes.Remove("copied");
        CopyGlyph.Data = CopyGeometry;
    }
}
