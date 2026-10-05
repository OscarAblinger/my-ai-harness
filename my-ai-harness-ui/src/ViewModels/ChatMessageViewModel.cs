using System;
using LiveMarkdown.Avalonia;
using Ablinger.MyAiHarness.UI.Models;

namespace Ablinger.MyAiHarness.UI.ViewModels;

/// <summary>
/// One entry in the chat transcript. The markdown text lives in <see cref="Content"/>, an
/// observable string builder handed straight to the message's <c>MarkdownRenderer</c>, so
/// streaming replies re-render incrementally.
/// </summary>
public partial class ChatMessageViewModel : ViewModelBase
{
    public ChatMessageViewModel(ChatRole role, string initialMarkdown = "")
    {
        Role = role;
        Timestamp = DateTimeOffset.Now;

        if (initialMarkdown.Length > 0)
        {
            Markdown = initialMarkdown;
            Content.Append(initialMarkdown);
        }
    }

    public ChatRole Role { get; }

    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Reserved for a future avatar; unused for now. Kept on the view model so adding one later
    /// is a view-only change.
    /// </summary>
    public string? AvatarSource { get; set; }

    public ObservableStringBuilder Content { get; } = new();

    /// <summary>
    /// Plain markdown source of the whole message — the copy button's target. Kept as a mirror of
    /// <see cref="Content"/> because the string builder exposes no text accessor; the constructor
    /// and <see cref="Append"/> are its only writers, so it cannot drift.
    /// </summary>
    public string Markdown { get; private set; } = string.Empty;

    public bool IsUser => Role == ChatRole.User;

    public bool IsAssistant => Role == ChatRole.Assistant;

    public string DisplayName => Role == ChatRole.User ? "You" : "Assistant";

    public string TimestampText => Timestamp.ToLocalTime().ToString("HH:mm");

    /// <summary>Raised whenever the markdown content changes (i.e. on every streamed chunk).</summary>
    public event EventHandler? Updated;

    public void Append(string chunk)
    {
        Markdown += chunk;
        Content.Append(chunk);
        Updated?.Invoke(this, EventArgs.Empty);
    }
}
