using CommunityToolkit.Mvvm.ComponentModel;

namespace my_ai_harness_ui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}