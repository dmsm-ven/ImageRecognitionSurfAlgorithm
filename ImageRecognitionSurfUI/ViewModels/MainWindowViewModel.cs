using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageRecognitionSurfLib;
using Microsoft.Win32;
using OpenCvSharp;
using System.Diagnostics;
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

    [RelayCommand(CanExecute = nameof(CanProcessImage))]
    private async Task ProcessImage()
    {
        Stopwatch sw = Stopwatch.StartNew();

        ResultImagePath = "";


        var iconFiles = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "icons"), "*.png", SearchOption.TopDirectoryOnly);

        try
        {
            recProcessor.PREDEFINED_ImreadMode = Enum.Parse<ImreadModes>(processOptionsViewModel.SelectedImreadMode);
            recProcessor.PREDEFINED_TemplateMatchMode = Enum.Parse<TemplateMatchModes>(processOptionsViewModel.SelectedTemplateMatchMode);
            recProcessor.PREDEFINED_RetrievalMode = Enum.Parse<RetrievalModes>(processOptionsViewModel.SelectedRetrievalMode);
            recProcessor.PREDEFINED_ContourApproximationMode = Enum.Parse<ContourApproximationModes>(processOptionsViewModel.SelectedContourApproximationMode);
            recProcessor.PREDEFINED_MatType = new MatType(int.Parse(processOptionsViewModel.SelectedMatType.Split("|")[0].Trim()));


            var result = await recProcessor.RecognizeDataToFile(storedOriginalSourcePath, iconFiles, maxItems: 12 + 36);

            App.Current.Dispatcher.Invoke(() =>
            {
                SourceImagePath = result.UpdatedScreenshotPath;
                ResultImagePath = result.MaskFilePath;

                Title = $"Распознавание выполнено за: {sw.Elapsed.TotalSeconds.ToString("F1")} сек.";
            });

        }
        catch (Exception ex)
        {
            Title = $"Ошибка: {ex.Message}";
        }
    }
}
