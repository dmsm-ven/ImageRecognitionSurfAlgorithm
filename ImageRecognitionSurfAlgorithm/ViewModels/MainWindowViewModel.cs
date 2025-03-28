using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ImageRecognitionSurfAlgorithm.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string sourceImagePath;

    [ObservableProperty]
    private string resultImagePath;

    public MainWindowViewModel()
    {

    }

    [RelayCommand]
    private async Task LoadSourceImage()
    {

    }
}
