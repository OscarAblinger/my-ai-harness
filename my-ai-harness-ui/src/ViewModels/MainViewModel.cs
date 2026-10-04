using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using my_ai_harness_ui.Models;
using my_ai_harness_ui.Services;

namespace my_ai_harness_ui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IChatReplySource _replySource;
    private CancellationTokenSource? _replyCancellation;

    public MainViewModel()
        : this(new MockChatReplySource())
    {
    }

    public MainViewModel(IChatReplySource replySource)
    {
        _replySource = replySource;
        Messages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMessages));
    }

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    public bool HasMessages => Messages.Count > 0;

    [ObservableProperty]
    public partial string Draft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Raised when a message was added or existing content changed, so the view can re-apply its
    /// scroll pinning.
    /// </summary>
    public event EventHandler? TranscriptChanged;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var prompt = Draft.Trim();
        if (prompt.Length == 0 || IsBusy)
            return;

        IsBusy = true;
        Draft = string.Empty;

        AppendMessage(new ChatMessageViewModel(ChatRole.User, prompt));
        var reply = AppendMessage(new ChatMessageViewModel(ChatRole.Assistant));

        _replyCancellation?.Cancel();
        _replyCancellation = new CancellationTokenSource();

        try
        {
            await foreach (var chunk in _replySource.StreamReplyAsync(prompt, _replyCancellation.Token))
            {
                reply.Append(chunk);
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer send, or the window is closing - nothing to report.
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(Draft);

    private ChatMessageViewModel AppendMessage(ChatMessageViewModel message)
    {
        message.Updated += OnMessageUpdated;
        Messages.Add(message);
        TranscriptChanged?.Invoke(this, EventArgs.Empty);
        return message;
    }

    private void OnMessageUpdated(object? sender, EventArgs e)
    {
        TranscriptChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnDraftChanged(string value)
    {
        SendCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        SendCommand.NotifyCanExecuteChanged();
    }
}
