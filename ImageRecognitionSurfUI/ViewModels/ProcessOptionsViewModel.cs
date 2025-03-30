using CommunityToolkit.Mvvm.ComponentModel;
using ImageRecognitionSurfLib;
using OpenCvSharp;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace ImageRecognitionSurfUI.ViewModels;

public partial class ProcessOptionsViewModel : ObservableValidator
{
    [ObservableProperty]
    private double threshold_Thresh = 0.8;

    [ObservableProperty]
    private double threshold_MaxVal = 256;

    [ObservableProperty]
    private string threshold_Type = ThresholdTypes.Otsu.ToString();

    [ObservableProperty]
    public string[] threshold_Types;

    [ObservableProperty]
    private double canny_Treshold1 = 200;

    [ObservableProperty]
    private double canny_Treshold2 = 256;

    [Range(3, 7, ErrorMessage = "Должно быть в пределах от 3 до 7")]
    [ObservableProperty]
    private int canny_AppertureSize = 3;

    [ObservableProperty]
    private int surftRecognizer_HessianThreshold = 100;

    [ObservableProperty]
    private bool canny_L2Gradient = true;

    [ObservableProperty]
    private bool useCannyOptions = true;

    [ObservableProperty]
    private bool useThresholdOptions = true;

    [ObservableProperty]
    private bool useConvertColorsOptions = true;

    private readonly MainWindowViewModel parentVm;
    private readonly OpenCvSharpProcessor recProcessor;

    [ObservableProperty]
    private FileInfo? selectedIconFile = null;

    async partial void OnSelectedIconFileChanged(FileInfo? newValue)
    {
        if (newValue is not null)
        {
            await parentVm.RotateAgnosticCheckCommand.ExecuteAsync(null);
        }
    }

    [ObservableProperty]
    private ObservableCollection<FileInfo> iconFiles = new();

    public ProcessOptionsViewModel(MainWindowViewModel parentVm)
    {
        this.parentVm = parentVm;
        this.PropertyChanged += MainWindowViewModel_PropertyChanged;

        Directory
            .GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "icons"))
            .Select(file => new FileInfo(file))
            .OrderBy(file => file.Name)
            .ToList().ForEach(file =>
        {
            IconFiles.Add(file);
        });
        SelectedIconFile = iconFiles.FirstOrDefault();

        Threshold_Types = Enum.GetNames<ThresholdTypes>().ToArray();
    }

    private async void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        await parentVm.RotateAgnosticCheckCommand.ExecuteAsync(null);
    }
}