using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageRecognitionSurfLib;
using Microsoft.Win32;
using System.IO;

namespace ImageRecognitionSurfUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IImageRecognitionProcessor recProcessor;

    [NotifyCanExecuteChangedFor(nameof(ProcessImageCommand))]
    [ObservableProperty]
    private string sourceImagePath = string.Empty;

    [ObservableProperty]
    private string resultImagePath = string.Empty;

    public bool CanProcessImage => !string.IsNullOrEmpty(SourceImagePath) && File.Exists(SourceImagePath);

    public MainWindowViewModel(IImageRecognitionProcessor recProcessor)
    {
        this.recProcessor = recProcessor;
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
        ResultImagePath = await recProcessor.RecognizeDataToFile(SourceImagePath);
        /*
        try
        {
            Stopwatch sw = Stopwatch.StartNew();
            ResultImagePath = await recProcessor.RecognizeDataToFile(SourceImagePath);
            MessageBox.Show($"Распознавание выполнено за: {sw.Elapsed.TotalSeconds.ToString("F1")} сек.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\r\n\r\n" + ex.StackTrace, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        */
    }
}
