using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using ImageRecognitionSurfLib;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace ImageRecognitionSurfUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private const int MEMORY_BUFFER_MAX_ITEMS = 30;

    private readonly LinkedList<long> memoryUsageBuffer;

    private readonly DispatcherTimer memoryUsageDisplayTimer;

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
        memoryUsageDisplayTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Send, (e, v) =>
        {
            long memoryUsageCurrent = Process.GetCurrentProcess().WorkingSet64;
            if (memoryUsageBuffer.Count == MEMORY_BUFFER_MAX_ITEMS)
            {
                memoryUsageBuffer.RemoveFirst();
            }
            long memoryUsageAvgPerMinute = memoryUsageBuffer.Sum() / (memoryUsageBuffer.Count > 0 ? memoryUsageBuffer.Count : 1);

            this.memoryUsageBuffer.AddLast(memoryUsageCurrent);
            Title = $"Использование памяти: {memoryUsageCurrent.Bytes().Humanize()} | В среднем за последнюю минуту: {memoryUsageAvgPerMinute.Bytes().Humanize()}";
        }, App.Current.Dispatcher);

        memoryUsageBuffer = new LinkedList<long>();
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

        memoryUsageDisplayTimer.Start();

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
        var icons = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "icons"), "*.png", SearchOption.TopDirectoryOnly);
        var resultFile = await recProcessor.RecognizeDataToFile(SourceImagePath, icons, 48);
        this.ResultImagePath = resultFile.UpdatedScreenshotPath;
    }
}
