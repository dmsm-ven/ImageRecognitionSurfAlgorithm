using ImageRecognitionSurfLib.Helpers;
using ImageRecognitionSurfLib.ImageMatcherProcessor;
using ImageRecognitionSurfLib.Models;
using OpenCvSharp;
using System.Collections.Concurrent;

namespace ImageRecognitionSurfLib;

public class OpenCvSharpProcessor
{
    public static readonly Size ICON_SIZE = new(55, 55);
    //public ImreadModes? PREDEFINED_ImreadMode { get; set; } = ImreadModes.Grayscale;
    public MatType PREDEFINED_MatType { get; set; } = MatType.CV_8UC1;
    public Rect DeskSpellsPanelBounds = new(new Point(670, 140), new Size(590, 700));
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



    public Task<List<string>> RecognizeAbilityNames(string filePath, IEnumerable<string> iconFiles, int maxItems)
    {
        return RecognizeAbilityNamesBase(filePath, maxItems, iconFiles, null);
    }

    public Task<List<string>> RecognizeAbilityNames(string filePath, IDictionary<string, byte[]> iconFilesData, int maxItems)
    {
        return RecognizeAbilityNamesBase(filePath, maxItems, null, iconFilesData);
    }

    private async Task<List<string>> RecognizeAbilityNamesBase(string filePath, int maxItems,
        IEnumerable<string> iconFiles,
        IDictionary<string, byte[]> iconFilesData)
    {
        if (string.IsNullOrWhiteSpace(filePath) || maxItems == 0)
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
            var iconsTask = iconFiles != null ? GetPredefinedItems(iconFiles) : GetPredefinedItems(iconFilesData);

            var icons = await iconsTask;

            var screenshotMat = BuildMat(filePath, isMain: true);

            var pointsInfo = await ExtractAllPositionsAsync(screenshotMat, icons, maxItems);

            List<string> abilitiesInCanvas = pointsInfo.Select(i => i.Title).ToList();

            return abilitiesInCanvas;
        }
        catch
        {

        }
        return Enumerable.Empty<string>().ToList();
    }

    public async Task<ScreenshotRecognizeResult> RecognizeDataToFile(string filePath, IEnumerable<string> iconFiles, int maxItems)
    {
        if (string.IsNullOrWhiteSpace(filePath) || iconFiles.Count() == 0 || maxItems == 0)
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
            var icons = await GetPredefinedItems(iconFiles);

            var screenshotMat = BuildMat(filePath, isMain: true);

            var pointsInfo = await ExtractAllPositionsAsync(screenshotMat, icons, maxItems);

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

    public async Task<ScreenshotRecognizeResult> RotateAgnosticCheck(string screenshotPath, string iconPath, int maxPoints)
    {
        var screenshotMat = BuildMat(screenshotPath, isMain: true);
        var iconMat = BuildMat(iconPath, isMain: false);

        string name = Path.Combine(Path.GetFileNameWithoutExtension(screenshotPath) + $"_result_{DateTime.Now.Ticks}" + Path.GetExtension(screenshotPath));
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "cache", name);

        try
        {
            var resultImage = await SurftRecognizer.DetectAndMatchFeaturesUsingSURF(screenshotMat, iconMat, Path.GetFileNameWithoutExtension(iconPath), maxPoints);
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

    private Mat? BuildMat(byte[]? fileData, bool isMain)
    {
        try
        {
            var mat = Cv2.ImDecode(fileData, ImreadMode);
            mat = ApplyMatOptions(mat, isMain);
            return mat;
        }
        catch
        {
            throw;
        }
        return null;
    }

    private Mat? BuildMat(string fileName, bool isMain)
    {
        try
        {
            var mat = new Mat(fileName, ImreadMode);
            mat = ApplyMatOptions(mat, isMain);
            return mat;
        }
        catch
        {
            throw;
        }
        return null;
    }

    private Mat? ApplyMatOptions(Mat? mat, bool isMain)
    {
        if (!isMain)
        {
            mat = mat.Resize(ICON_SIZE);
        }

        if (UseBlurOptions)
        {
            mat = mat.Blur(new Size(BlurSize, BlurSize));
        }

        if (UseCannyOptions)
        {
            mat.ConvertTo(mat, PREDEFINED_MatType);
            mat = mat.Canny(Canny_Treshold1, Canny_Treshold2, Canny_AppertureSize, Canny_L2Gradient);
        }

        if (UseThresholdOptions)
        {
            mat.ConvertTo(mat, PREDEFINED_MatType);
            mat = mat.Threshold(Threshold_Thresh, Threshold_MaxVal, Threshold_Type);
        }
        if (isMain)
        {
            mat = new Mat(mat, DeskSpellsPanelBounds);
        }
        return mat;
    }

    private async Task Recognize(string filePath, string outputFile, IEnumerable<string> iconFiles, int maxItems)
    {
        try
        {
            var icons = await GetPredefinedItems(iconFiles);

            var screenshotMat = BuildMat(filePath, isMain: true);

            var pointsInfo = await ExtractAllPositionsAsync(screenshotMat, icons, maxItems);

            screenshotMat.DrawSearchResultBorders(pointsInfo, drawTotal: false, drawNumber: true, drawList: false); // static helper метод

            screenshotMat.SaveImage(outputFile);
        }
        catch
        {
            throw;
        }
    }

    private async Task<List<AbilityKnownLocation>> ExtractAllPositionsAsync(Mat scrMat, IEnumerable<AbilityMatWrapper> icons, int maxItems)
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

        var sortedByWeight = bag.OrderBy(i => i.Weight).Take(maxItems).ToList();
        //await SurftRecognizer.WriteLogLine(JsonSerializer.Serialize(sortedByWeight));

        return sortedByWeight;
    }

    private async Task<AbilityMatWrapper[]> GetPredefinedItems(IDictionary<string, byte[]> iconFiles)
    {
        var list = new List<AbilityMatWrapper>();

        foreach (var file in iconFiles)
        {
            Mat? mat = BuildMat(file.Value, isMain: false);

            list.Add(new AbilityMatWrapper()
            {
                ImageName = Path.GetFileNameWithoutExtension(file.Key),
                MatValue = mat
            });
        }
        return list.ToArray();
    }

    private async Task<AbilityMatWrapper[]> GetPredefinedItems(IEnumerable<string> iconFiles)
    {
        var list = new List<AbilityMatWrapper>();

        foreach (var file in iconFiles)
        {
            Mat? mat = BuildMat(file, isMain: false);

            list.Add(new AbilityMatWrapper()
            {
                ImageName = Path.GetFileNameWithoutExtension(file),
                MatValue = mat
            });
        }
        return list.ToArray();
    }
}
