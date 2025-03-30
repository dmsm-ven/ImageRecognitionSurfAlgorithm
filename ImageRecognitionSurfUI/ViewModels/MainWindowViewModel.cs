using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageRecognitionSurfLib;
using Microsoft.Win32;
using OpenCvSharp;
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
        var options = ProcessOptionsViewModel;
        if (options is null || !File.Exists(ProcessOptionsViewModel?.SelectedIconFile?.FullName))
        {
            return;
        }

        recProcessor.Canny_L2Gradient = options.Canny_L2Gradient;
        recProcessor.Canny_Treshold2 = options.Canny_Treshold2;
        recProcessor.Canny_Treshold1 = options.Canny_Treshold1;
        recProcessor.Threshold_Thresh = options.Threshold_Thresh;
        recProcessor.Threshold_MaxVal = options.Threshold_MaxVal;
        recProcessor.Threshold_Type = Enum.Parse<ThresholdTypes>(options.Threshold_Type.ToString());
        recProcessor.Canny_AppertureSize = options.Canny_AppertureSize;
        recProcessor.UseCannyOptions = options.UseCannyOptions;
        recProcessor.UseConvertToGrayOptions = recProcessor.UseConvertToGrayOptions;
        recProcessor.UseThresholdOptions = recProcessor.UseThresholdOptions;
        recProcessor.SurftRecognizer_HessianThreshold = recProcessor.SurftRecognizer_HessianThreshold;

        string resultFile = await recProcessor.RotateAgnosticCheck(SourceImagePath, ProcessOptionsViewModel?.SelectedIconFile?.FullName, maxPoints: 1);

        this.ResultImagePath = resultFile;
    }

    [RelayCommand(CanExecute = nameof(CanProcessImage))]
    private async Task ProcessImage()
    {
        var icons = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "icons"), "*.png", SearchOption.TopDirectoryOnly);
        var resultFile = await recProcessor.RecognizeDataToFile(SourceImagePath, icons, 48);
        this.ResultImagePath = resultFile.UpdatedScreenshotPath;
    }
}
