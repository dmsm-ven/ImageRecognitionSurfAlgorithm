using ImageRecognitionSurfLib.ImageMatcherProcessor;
using ImageRecognitionSurfLib.Models;
using OpenCvSharp;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ImageRecognitionSurfLib;

public class OpenCvSharpProcessor
{
    //public ImreadModes? PREDEFINED_ImreadMode { get; set; } = ImreadModes.Grayscale;
    public MatType PREDEFINED_MatType { get; set; } = MatType.CV_8UC1;
    public Rect DeskSpellsPanelBounds = new(new Point(670, 140), new Size(590, 700));

    public bool UseConvertToGrayOptions { get; set; } = true;
    public bool UseThresholdOptions { get; set; } = true;
    public double Threshold_Thresh { get; set; } = 0.8;
    public double Threshold_MaxVal { get; set; } = 256;
    public ThresholdTypes Threshold_Type { get; set; } = ThresholdTypes.Otsu;

    public bool UseCannyOptions { get; set; } = true;
    public double Canny_Treshold1 { get; set; } = 200;
    public double Canny_Treshold2 { get; set; } = 256;
    public int Canny_AppertureSize { get; set; } = 3;
    public bool Canny_L2Gradient { get; set; } = true;

    public int SurftRecognizer_HessianThreshold { get; set; } = 100;
    public NormTypes SurftRecognizer_NormType { get; set; } = NormTypes.L2;

    public async Task<ScreenshotRecognizeResult> RecognizeDataToFile(string filePath, IEnumerable<string> iconFiles, int maxItems)
    {
        SurftRecognizer.HessianThreshold = SurftRecognizer_HessianThreshold;
        SurftRecognizer.NormType = SurftRecognizer_NormType;

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
            await Recognize(filePath, outputFileResult, iconFiles, maxItems);
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

    private Mat? BuildMat(string fileName, bool isMain)
    {
        try
        {
            var mat = new Mat(fileName, UseConvertToGrayOptions ? ImreadModes.Grayscale : ImreadModes.Color);

            if (UseConvertToGrayOptions)
            {
                mat.ConvertTo(mat, PREDEFINED_MatType);
                mat = mat.CvtColor(ColorConversionCodes.GRAY2BGR);
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
        catch (OpenCvSharp.OpenCVException)
        {
            throw;
        }
        catch
        {
            throw;
        }
        return null;
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

    private async Task Recognize(string filePath, string outputFile, IEnumerable<string> iconFiles, int maxItems)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(outputFile) || iconFiles.Count() == 0 || maxItems == 0)
        {
            return;
        }

        try
        {
            /*var icons = await GetPredefinedItems(iconFiles);

            var screenshotMat = new Mat(filePath, ImreadModes.Color);
            screenshotMat.ConvertTo(screenshotMat, PREDEFINED_MatType);
            screenshotMat = new Mat(screenshotMat, DeskSpellsPanelBounds);

            var pointsInfo = await ExtractAllPositionsAsync(screenshotMat, icons, maxItems);

            screenshotMat.DrawSearchResultBorders(pointsInfo); // static helper метод

            screenshotMat.SaveImage(outputFile);
            */
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
        await SurftRecognizer.WriteLogLine(JsonSerializer.Serialize(sortedByWeight));

        return sortedByWeight;
    }

    private async Task<AbilityMatWrapper[]> GetPredefinedItems(IEnumerable<string> iconFiles)
    {
        throw new NotImplementedException();
        /*
        var list = new List<AbilityMatWrapper>();

        foreach (var file in iconFiles)
        {
            var mat = BuildMat(file, isMain: false);

            list.Add(new AbilityMatWrapper()
            {
                ImageName = Path.GetFileNameWithoutExtension(file),
                MatValue = mat
            });
        }

        return list.ToArray();
        */
    }
}
