using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageRecognitionSurfLib;
using Microsoft.Win32;
using OpenCvSharp;
using System.IO;
using static ImageRecognitionSurfLib.OpenCvSharpProcessor;

namespace ImageRecognitionSurfUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly OpenCvSharpProcessor recProcessor;
    private readonly ISettingsStorage settingsStorage;

    private string? storedOriginalSourcePath = null;

    [ObservableProperty]
    private string title = "CV recognizer";

    [ObservableProperty]
    private ProcessOptionsViewModel processOptionsViewModel;

    [ObservableProperty]
    private string sourceImagePath = string.Empty;

    [ObservableProperty]
    private string resultImagePath = string.Empty;

    [ObservableProperty]
    private string errorMessageText = string.Empty;

    public bool CanProcessImage => !string.IsNullOrEmpty(SourceImagePath) && File.Exists(SourceImagePath);

    public MainWindowViewModel(OpenCvSharpProcessor recProcessor, ISettingsStorage settingsStorage)
    {
        this.recProcessor = recProcessor;
        this.settingsStorage = settingsStorage;
    }

    [RelayCommand]
    private async Task Loaded()
    {
        ProcessOptionsViewModel = new ProcessOptionsViewModel(this, settingsStorage);

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

    private void ApplySelectedOptions()
    {
        var options = ProcessOptionsViewModel;

        recProcessor.UseCannyOptions = options.UseCannyOptions;
        recProcessor.Canny_L2Gradient = options.Canny_L2Gradient;
        recProcessor.Canny_Treshold2 = options.Canny_Treshold2;
        recProcessor.Canny_Treshold1 = options.Canny_Treshold1;
        recProcessor.Canny_AppertureSize = options.Canny_AppertureSize;
        recProcessor.UseThresholdOptions = options.UseThresholdOptions;
        recProcessor.Threshold_Thresh = options.Threshold_Thresh;
        recProcessor.Threshold_MaxVal = options.Threshold_MaxVal;
        recProcessor.Threshold_Type = Enum.Parse<ThresholdTypes>(options.Threshold_Type);
        recProcessor.UseBlurOptions = options.UseBlurOptions;
        recProcessor.BlurSize = options.BlurSize;
        recProcessor.ImreadMode = Enum.Parse<ImreadModes>(options.Imread_Mode);
        recProcessor.SurftRecognizer_DistanceMinThreshold = options.SurftRecognizer_DistanceMinThreshold;
        recProcessor.SurftRecognizer_HessianThreshold = options.SurftRecognizer_HessianThreshold;
        recProcessor.SurftRecognizer_NormType = Enum.Parse<NormTypes>(options.SurftRecognizer_NormType);
    }

    [RelayCommand]
    private async Task FindSingle()
    {
        var options = ProcessOptionsViewModel;

        if (options is null || !File.Exists(options?.SelectedIconFile?.FullName))
        {
            return;
        }

        ApplySelectedOptions();

        try
        {
            var result = await recProcessor.RecognizeSingleAbility(SourceImagePath,
                options?.SelectedIconFile?.FullName,
                AbilityPanel.Ultimates);

            if (!result.HasErrors)
            {
                ResultImagePath = result.UpdatedScreenshotPath;
                ErrorMessageText = "";
            }
            else
            {
                ErrorMessageText = result.ErrorMessage;
                ResultImagePath = "";
            }
        }
        catch (Exception ex)
        {
            ErrorMessageText = ex.Message;
            ResultImagePath = "";
        }
    }

    [RelayCommand]
    private async Task FindAll()
    {
        ApplySelectedOptions();

        try
        {
            var icons = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "icons"), "*.png", SearchOption.TopDirectoryOnly);
            var result = await recProcessor.RecognizeDataToFile(SourceImagePath, icons, AbilityPanel.Ultimates);

            if (!result.HasErrors)
            {
                ResultImagePath = result.UpdatedScreenshotPath;
                ErrorMessageText = "";
            }
            else
            {
                ErrorMessageText = result.ErrorMessage;
                ResultImagePath = "";
            }
        }
        catch (Exception ex)
        {
            ErrorMessageText = ex.Message;
            ResultImagePath = "";
        }
    }

}
