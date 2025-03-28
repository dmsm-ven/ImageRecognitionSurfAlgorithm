using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;

namespace ImageRecognitionSurfAlgorithm.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessImageCommand))]
    private string sourceImagePath = string.Empty;

    [ObservableProperty]
    private string resultImagePath = string.Empty;

    public bool CanProcessImage => !string.IsNullOrEmpty(SourceImagePath) && File.Exists(SourceImagePath);

    public MainWindowViewModel()
    {

    }

    [RelayCommand]
    private async Task Loaded()
    {
        var resourceFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.png", SearchOption.TopDirectoryOnly);
        if (resourceFiles.Any())
        {
            this.SourceImagePath = resourceFiles.First();
        }
    }

    [RelayCommand]
    private async Task LoadSourceImage()
    {
        var ofd = new OpenFileDialog()
        {
            Title = "Выберите изображение",
            Filter = "PNG file|*.png"
        };
        if (ofd.ShowDialog() == true)
        {
            SourceImagePath = ofd.FileName;
        }
    }

    [RelayCommand(CanExecute = nameof(CanProcessImage))]
    private async Task ProcessImage()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}
