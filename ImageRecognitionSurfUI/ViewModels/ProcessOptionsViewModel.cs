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
    public string[] threshold_Types;

    [ObservableProperty]
    public string[] norm_Types;

    [ObservableProperty]
    public string[] imread_Modes;

    [ObservableProperty]
    private double surftRecognizer_DistanceMinThreshold = 0.7;

    [ObservableProperty]
    private double threshold_Thresh = 0.8;

    [ObservableProperty]
    private double threshold_MaxVal = 256;

    [ObservableProperty]
    private string threshold_Type = ThresholdTypes.Otsu.ToString();

    [ObservableProperty]
    private string imread_Mode = ImreadModes.Color.ToString();

    [ObservableProperty]
    private string surftRecognizer_NormType = NormTypes.L2.ToString();

    [ObservableProperty]
    private double canny_Treshold1 = 200;

    [ObservableProperty]
    private double canny_Treshold2 = 256;

    [ObservableProperty]
    private double blurSize = 1;

    [Range(3, 7, ErrorMessage = "Должно быть в пределах от 3 до 7")]
    [ObservableProperty]
    private int canny_AppertureSize = 3;

    [ObservableProperty]
    private int surftRecognizer_HessianThreshold = 120;

    [ObservableProperty]
    private bool canny_L2Gradient = true;

    [ObservableProperty]
    private bool useCannyOptions = false;

    [ObservableProperty]
    private bool useThresholdOptions = false;

    [ObservableProperty]
    private bool useConvertColorsOptions = true;

    [ObservableProperty]
    private bool useBlurOptions = true;

    private readonly MainWindowViewModel parentVm;
    private readonly ISettingsStorage settingsStorage;
    private readonly OpenCvSharpProcessor recProcessor;

    [ObservableProperty]
    private FileInfo? selectedIconFile = null;

    async partial void OnSelectedIconFileChanged(FileInfo? newValue)
    {
        if (newValue is not null)
        {
            await parentVm.FindSingleCommand.ExecuteAsync(null);
        }
    }

    [ObservableProperty]
    private ObservableCollection<FileInfo> iconFiles = new();

    public ProcessOptionsViewModel(MainWindowViewModel parentVm, ISettingsStorage settingsStorage)
    {
        this.parentVm = parentVm;
        this.settingsStorage = settingsStorage;
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
        Norm_Types = Enum.GetNames<NormTypes>().ToArray();
        Imread_Modes = Enum.GetNames<ImreadModes>().ToArray();

        LoadOptions();

        this.PropertyChanged += MainWindowViewModel_PropertyChanged;
    }

    private void LoadOptions()
    {
        if (!File.Exists("settings.json"))
        {
            return;
        }
        BlurSize = settingsStorage.GetByKey<double>(nameof(BlurSize));
        Threshold_MaxVal = settingsStorage.GetByKey<double>(nameof(Threshold_MaxVal));
        Threshold_Thresh = settingsStorage.GetByKey<double>(nameof(Threshold_Thresh));
        Threshold_Type = settingsStorage.GetByKey<string>(nameof(Threshold_Type));
        UseBlurOptions = settingsStorage.GetByKey<bool>(nameof(UseBlurOptions));
        UseCannyOptions = settingsStorage.GetByKey<bool>(nameof(UseCannyOptions));
        UseThresholdOptions = settingsStorage.GetByKey<bool>(nameof(UseThresholdOptions));
        UseConvertColorsOptions = settingsStorage.GetByKey<bool>(nameof(UseConvertColorsOptions));
        Canny_AppertureSize = settingsStorage.GetByKey<int>(nameof(Canny_AppertureSize));
        Canny_L2Gradient = settingsStorage.GetByKey<bool>(nameof(Canny_L2Gradient));
        Canny_Treshold1 = settingsStorage.GetByKey<double>(nameof(Canny_Treshold1));
        Canny_Treshold2 = settingsStorage.GetByKey<double>(nameof(Canny_Treshold2));
        SurftRecognizer_DistanceMinThreshold = settingsStorage.GetByKey<double>(nameof(SurftRecognizer_DistanceMinThreshold));
        SurftRecognizer_HessianThreshold = settingsStorage.GetByKey<int>(nameof(SurftRecognizer_HessianThreshold));
        SurftRecognizer_NormType = settingsStorage.GetByKey<string>(nameof(SurftRecognizer_NormType));
    }

    private void SaveOptions()
    {
        var dic = new Dictionary<string, object>
        {
            { nameof(BlurSize), BlurSize },
            { nameof(Threshold_MaxVal), Threshold_MaxVal },
            { nameof(Threshold_Thresh), Threshold_Thresh },
            { nameof(Threshold_Type), Threshold_Type },
            { nameof(UseBlurOptions), UseBlurOptions },
            { nameof(UseCannyOptions), UseCannyOptions },
            { nameof(UseThresholdOptions), UseThresholdOptions },
            { nameof(UseConvertColorsOptions), UseConvertColorsOptions },
            { nameof(Canny_AppertureSize), Canny_AppertureSize },
            { nameof(Canny_L2Gradient), Canny_L2Gradient },
            { nameof(Canny_Treshold1), Canny_Treshold1 },
            { nameof(Canny_Treshold2), Canny_Treshold2 },
            { nameof(SurftRecognizer_DistanceMinThreshold), SurftRecognizer_DistanceMinThreshold },
            { nameof(SurftRecognizer_HessianThreshold), SurftRecognizer_HessianThreshold },
            { nameof(SurftRecognizer_NormType), SurftRecognizer_NormType }
        };

        settingsStorage.SaveAll(dic);
    }

    private async void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var property = e.PropertyName;
        await parentVm.FindSingleCommand.ExecuteAsync(null);
        SaveOptions();
    }
}