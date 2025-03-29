using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageRecognitionSurfLib;
using Microsoft.Win32;
using System.IO;

namespace ImageRecognitionSurfUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly OpenCvSharpProcessor recProcessor;

    private string? storedOriginalSourcePath = null;

    [ObservableProperty]
    private string title = "CV recognizer";

    [ObservableProperty]
    private ProcessOptionsViewModel processOptionsViewModel;

    [NotifyCanExecuteChangedFor(nameof(ProcessImageCommand))]
    [ObservableProperty]
    private string sourceImagePath = string.Empty;

    [ObservableProperty]
    private string resultImagePath = string.Empty;

    public bool CanProcessImage => !string.IsNullOrEmpty(SourceImagePath) && File.Exists(SourceImagePath);

    public MainWindowViewModel(OpenCvSharpProcessor recProcessor)
    {
        this.recProcessor = recProcessor;
    }

    [RelayCommand]
    private async Task Loaded()
    {
        ProcessOptionsViewModel = new ProcessOptionsViewModel(this);

        //Оригиналы скриншотов
        string screenshotsFolder = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        if (!Directory.Exists(screenshotsFolder))
        {
            Directory.CreateDirectory(screenshotsFolder);
        }
        var resourceFiles = Directory.GetFiles(screenshotsFolder, "*.png", SearchOption.TopDirectoryOnly);
        if (resourceFiles.Any())
        {
            this.SourceImagePath = resourceFiles.First();
            storedOriginalSourcePath = this.SourceImagePath;
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
            storedOriginalSourcePath = SourceImagePath;
        }
    }

    [RelayCommand]
    private async Task RotateAgnosticCheck()
    {
        string testIcon = ProcessOptionsViewModel.SelectedIconFile.FullName;
        string resultFile = await recProcessor.RotateAgnosticCheck(SourceImagePath, testIcon);
        this.ResultImagePath = resultFile;
    }

    [RelayCommand(CanExecute = nameof(CanProcessImage))]
    private async Task ProcessImage()
    {

    }
}
