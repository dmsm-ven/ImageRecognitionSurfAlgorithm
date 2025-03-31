using ImageRecognitionSurfLib.Helpers;
using ImageRecognitionSurfLib.ImageMatcherProcessor;
using ImageRecognitionSurfLib.Models;
using OpenCvSharp;
using System.Collections.Concurrent;
using System.Data;

namespace ImageRecognitionSurfLib;

public partial class OpenCvSharpProcessor
{
    //public ImreadModes? PREDEFINED_ImreadMode { get; set; } = ImreadModes.Grayscale;
    public MatType PREDEFINED_MatType { get; set; } = MatType.CV_8UC1;
    public ImreadModes ImreadMode { get; set; } = ImreadModes.Unchanged;
    public bool UseThresholdOptions { get; set; } = true;
    public double Threshold_Thresh { get; set; } = 0.8;
    public double Threshold_MaxVal { get; set; } = 256;
    public ThresholdTypes Threshold_Type { get; set; } = ThresholdTypes.Otsu;
    public bool UseCannyOptions { get; set; } = true;
    public double Canny_Treshold1 { get; set; } = 200;
    public double Canny_Treshold2 { get; set; } = 256;
    public int Canny_AppertureSize { get; set; } = 3;
    public bool Canny_L2Gradient { get; set; } = true;
    public bool UseBlurOptions { get; set; } = false;
    public double BlurSize { get; set; } = 3;
    public int SurftRecognizer_HessianThreshold
    {
        get => SurftRecognizer.HessianThreshold;
        set => SurftRecognizer.HessianThreshold = value;
    }
    public double SurftRecognizer_DistanceMinThreshold
    {
        get => SurftRecognizer.DistanceMinThreshold;
        set => SurftRecognizer.DistanceMinThreshold = value;
    }
    public NormTypes SurftRecognizer_NormType
    {
        get => SurftRecognizer.NormType;
        set => SurftRecognizer.NormType = value;
    }

    private MatModelBuilder matModelBuilder;

    public OpenCvSharpProcessor()
    {
        matModelBuilder = new MatModelBuilder(this);
    }

    public async Task<ScreenshotRecognizeResult> RecognizeSingleAbility(string screenshotPath, string iconPath, AbilityPanel panel)
    {
        return await RecognizeSingleAbilityBase(screenshotPath, iconPath, panel);
    }

    private async Task<ScreenshotRecognizeResult> RecognizeSingleAbilityBase(string screenshotPath, string iconPath, AbilityPanel panel)
    {
        var screenshotMat = matModelBuilder.BuildMat(File.ReadAllBytes(screenshotPath), panel);
        var iconMat = matModelBuilder.BuildMat(File.ReadAllBytes(iconPath), panel: null);

        string name = Path.Combine(Path.GetFileNameWithoutExtension(screenshotPath) + $"_result_{DateTime.Now.Ticks}" + Path.GetExtension(screenshotPath));
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "cache", name);

        try
        {
            var resultImage = await SurftRecognizer.DetectAndMatchFeaturesUsingSURF(screenshotMat, iconMat, Path.GetFileNameWithoutExtension(iconPath), 1);
            resultImage.SaveImage(outputPath);
            return new ScreenshotRecognizeResult()
            {
                UpdatedScreenshotPath = outputPath
            };
        }
        catch (Exception ex)
        {
            return new ScreenshotRecognizeResult()
            {
                HasErrors = true,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<string>> RecognizeAbilityNames(string filePath, IEnumerable<string> iconFiles, AbilityPanel panel)
    {
        ConcurrentDictionary<string, byte[]> bag = new();
        var iconFilesDataTasks = iconFiles.Select(async (i) => await Task.Run(async () =>
        {
            var data = await File.ReadAllBytesAsync(i);
            bag[Path.GetFileNameWithoutExtension(i)] = data;
        }));

        await Task.WhenAll(iconFilesDataTasks);

        return await RecognizeAbilityNamesBase(filePath, bag.OrderBy(kvp => kvp.Value).ToDictionary(), panel);
    }

    private async Task<List<string>> RecognizeAbilityNamesBase(string filePath,
        IDictionary<string, byte[]> iconFilesData,
        AbilityPanel panel)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Enumerable.Empty<string>().ToList();
        }

        string ext = DateTime.Now.Ticks + Path.GetExtension(filePath);

        string outputFileResult = Path.Combine(Directory.GetCurrentDirectory(), "cache",
            Path.GetFileNameWithoutExtension(filePath) + $"_result_{ext}");

        string dir = Path.GetDirectoryName(outputFileResult);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        try
        {

            var icons = await matModelBuilder.GetPredefinedItems(iconFilesData);

            var screenshotMat = matModelBuilder.BuildMat(File.ReadAllBytes(filePath), panel);

            var pointsInfo = await ExtractAllPositionsAsync(screenshotMat, icons, panel);

            List<string> abilitiesInCanvas = pointsInfo.Select(i => i.Title).ToList();

            return abilitiesInCanvas;
        }
        catch
        {

        }
        return Enumerable.Empty<string>().ToList();
    }

    public async Task<ScreenshotRecognizeResult> RecognizeDataToFile(string filePath, IEnumerable<string> iconFiles, AbilityPanel panel)
    {
        if (string.IsNullOrWhiteSpace(filePath) || iconFiles.Count() == 0)
        {
            return new ScreenshotRecognizeResult()
            {
                HasErrors = true,
                ErrorMessage = "Ошибка инициализации"
            };
        }

        string ext = DateTime.Now.Ticks + Path.GetExtension(filePath);

        string outputFileResult = Path.Combine(Directory.GetCurrentDirectory(), "cache",
            Path.GetFileNameWithoutExtension(filePath) + $"_result_{ext}");

        string dir = Path.GetDirectoryName(outputFileResult);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        try
        {
            var icons = await matModelBuilder.GetPredefinedItems(iconFiles);

            var screenshotMat = matModelBuilder.BuildMat(File.ReadAllBytes(filePath), panel);

            var pointsInfo = await ExtractAllPositionsAsync(screenshotMat, icons, panel);

            screenshotMat.DrawSearchResultBorders(pointsInfo, drawTotal: false, drawNumber: true, drawList: false);

            screenshotMat.SaveImage(outputFileResult);

            return new ScreenshotRecognizeResult()
            {
                UpdatedScreenshotPath = outputFileResult
            };
        }
        catch (Exception ex)
        {
            return new ScreenshotRecognizeResult()
            {
                HasErrors = true,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<List<AbilityKnownLocation>> ExtractAllPositionsAsync(Mat scrMat, IEnumerable<AbilityMatWrapper> icons, AbilityPanel panel)
    {
        var matcher = new SURFImageMatcherProcessor();

        ConcurrentBag<AbilityKnownLocation> bag = new();

        var tasks = icons.Select(async (template) =>
        {
            Mat srcClone = scrMat.Clone();
            var knownLocation = await matcher.GetKnownLocation(srcClone, template.MatValue, template.ImageName);

            if (knownLocation != null)
            {
                bag.Add(knownLocation);
            }
            else
            {
                await SurftRecognizer.WriteLogLine(template.ImageName + " was null");
            }

            srcClone.Dispose();
            template.MatValue?.Dispose();
        });

        await Task.WhenAll(tasks.ToArray());

        int takeCount = matModelBuilder.AbilityPanelToMaxItemsCount(panel);
        var sortedByWeight = bag.OrderBy(i => i.Weight).Take(takeCount).ToList();
        //await SurftRecognizer.WriteLogLine(JsonSerializer.Serialize(sortedByWeight));

        return sortedByWeight;
    }
}

