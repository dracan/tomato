using CommunityToolkit.Mvvm.ComponentModel;

namespace Tomato.ViewModels;

/// <summary>
/// Main ViewModel that hosts the TimerViewModel and other components.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private TimerViewModel _timerViewModel;

    public MainViewModel(TimerViewModel timerViewModel)
    {
        _timerViewModel = timerViewModel;
    }
}
