using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit.TextMate;
using my_ai_harness_ui.ViewModels;
using TextMateSharp.Grammars;

namespace my_ai_harness_ui.Views;

public partial class MessageComposerView : UserControl
{
    private MainViewModel? _viewModel;
    private bool _syncingDraft;

    public MessageComposerView()
    {
        InitializeComponent();

        var registry = new RegistryOptions(ThemeName.DarkPlus);
        var installation = Editor.InstallTextMate(registry);
        var markdown = registry.GetLanguageByExtension(".md");
        installation.SetGrammar(registry.GetScopeByLanguageId(markdown.Id));

        // Default is true: TextView then inflates its scroll extent by (viewport - one line),
        // which shows a scrollbar while the editor is still growing and lets the view scroll a
        // full screen past the last line. The composer must stop exactly at its content.
        Editor.Options.AllowScrollBelowDocument = false;

        // TextEditor.Text is a plain CLR property (not an Avalonia property), so the draft is
        // mirrored by hand in both directions instead of through a binding.
        Editor.TextChanged += OnEditorTextChanged;
        DataContextChanged += OnDataContextChanged;

        // Tunnel + handledEventsToo: Ctrl+Enter must win even while the text area has focus.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public void FocusEditor()
    {
        Editor.TextArea.Focus();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as MainViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            PushDraftToEditor();
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_syncingDraft || _viewModel is null || _viewModel.Draft == Editor.Text)
            return;

        _syncingDraft = true;
        try
        {
            _viewModel.Draft = Editor.Text;
        }
        finally
        {
            _syncingDraft = false;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Draft))
            PushDraftToEditor();
    }

    private void PushDraftToEditor()
    {
        if (_syncingDraft || _viewModel is null || Editor.Text == _viewModel.Draft)
            return;

        _syncingDraft = true;
        try
        {
            Editor.Text = _viewModel.Draft;
        }
        finally
        {
            _syncingDraft = false;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        if (_viewModel is null)
            return;

        e.Handled = true;

        if (_viewModel.SendCommand.CanExecute(null))
            _viewModel.SendCommand.Execute(null);
    }
}
