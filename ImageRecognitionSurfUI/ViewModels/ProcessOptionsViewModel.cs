using CommunityToolkit.Mvvm.ComponentModel;
using ImageRecognitionSurfLib;
using OpenCvSharp;

namespace ImageRecognitionSurfUI.ViewModels;

public partial class ProcessOptionsViewModel : ObservableObject
{
    private readonly MainWindowViewModel parentVm;
    private readonly OpenCvSharpProcessor recProcessor;

    public static string[] GetAvailableThresholdTypes => Enum.GetNames<ThresholdTypes>().ToArray();

    public static string[] GetAvailableTemplateMatchModes => Enum.GetNames<TemplateMatchModes>().ToArray();

    public static string[] GetAvailableImreadModes => Enum.GetNames<ImreadModes>().ToArray();

    public static string[] GetAvailableRetrievalModes => Enum.GetNames<RetrievalModes>().ToArray();

    public static string[] GetAvailableContourApproximationModes => Enum.GetNames<ContourApproximationModes>().ToArray();

    public static string[] GetAvailableMatTypes
    {
        get
        {
            var list = new MatType[] {
                MatType.CV_USRTYPE1,
                MatType.CV_8S,
                MatType.CV_8U,
                MatType.CV_32S,
                MatType.CV_16S,
                MatType.CV_64F,
                MatType.CV_32F,
                MatType.CV_16U,
                MatType.CV_16S,
                MatType.CV_8UC1,
                MatType.CV_8UC2,
                MatType.CV_8UC3,
                MatType.CV_8UC4,
                MatType.CV_8SC1,
                MatType.CV_8SC2,
                MatType.CV_8SC3,
                MatType.CV_8SC4,
                MatType.CV_16UC1,
                MatType.CV_16UC2,
                MatType.CV_16UC3,
                MatType.CV_16UC4,
                MatType.CV_16SC1,
                MatType.CV_16SC2,
                MatType.CV_16SC3,
                MatType.CV_16SC4,
                MatType.CV_32SC1,
                MatType.CV_32SC2,
                MatType.CV_32SC3,
                MatType.CV_32SC4,
                MatType.CV_32FC1,
                MatType.CV_32FC2,
                MatType.CV_32FC3,
                MatType.CV_32FC4,
                MatType.CV_64FC1,
                MatType.CV_64FC2,
                MatType.CV_64FC3,
                MatType.CV_64FC4,
            };

            return list
                .Select(i => new
                {
                    Code = i.ToInt32(),
                    Name = i.ToInt32() + $" | ({i.ToString()})"
                })
                .DistinctBy(i => i.Code)
                .OrderBy(i => i.Code)
                .Select(i => i.Name)
                .ToArray();
        }
    }

    [ObservableProperty]
    private string selectedMatType = GetAvailableMatTypes.First();

    [ObservableProperty]
    private string selectedThresholdType = GetAvailableThresholdTypes.Single(i => i == "Otsu");

    [ObservableProperty]
    private string selectedTemplateMatchMode = GetAvailableTemplateMatchModes.Single(i => i == "SqDiffNormed");

    [ObservableProperty]
    private string selectedImreadMode = GetAvailableImreadModes.Single(i => i == "Color");

    [ObservableProperty]
    private string selectedRetrievalMode = GetAvailableRetrievalModes.Single(i => i == "CComp");

    [ObservableProperty]
    private string selectedContourApproximationMode = GetAvailableContourApproximationModes.Single(i => i == "ApproxSimple");

    [ObservableProperty]
    private string tresholdMaxValue = "255";

    [ObservableProperty]
    private string treshold = "0.8";

    public ProcessOptionsViewModel(MainWindowViewModel parentVm)
    {
        this.parentVm = parentVm;
        this.PropertyChanged += MainWindowViewModel_PropertyChanged;
    }

    private async void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        await parentVm.ProcessImageCommand.ExecuteAsync(null);
    }
}