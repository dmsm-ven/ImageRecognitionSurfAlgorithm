using OpenCvSharp;
using System.Collections.Concurrent;

namespace ImageRecognitionSurfLib;
public class OpenCvSharpProcessor
{
    public TemplateMatchModes? PREDEFINED_TemplateMatchMode { get; set; } = null;
    public ImreadModes? PREDEFINED_ImreadMode { get; set; } = null;
    public RetrievalModes? PREDEFINED_RetrievalMode { get; set; } = null;
    public ContourApproximationModes? PREDEFINED_ContourApproximationMode { get; set; } = null;
    public MatType PREDEFINED_MatType { get; set; } = MatType.CV_8U;

    public async Task<ScreenshotRecognizeResult> RecognizeDataToFile(string filePath, IEnumerable<string> iconFiles, int maxItems)
    {
        string ext = DateTime.Now.Ticks + Path.GetExtension(filePath);

        string outputFileResult = Path.Combine(Directory.GetCurrentDirectory(), "cache",
            Path.GetFileNameWithoutExtension(filePath) + $"_result_{ext}");

        string outputFileMask = Path.Combine(Directory.GetCurrentDirectory(), "cache",
    Path.GetFileNameWithoutExtension(filePath) + $"_mask_{ext}");

        string dir = Path.GetDirectoryName(outputFileResult);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await Recognize(filePath, outputFileResult, outputFileMask, iconFiles, maxItems);

        return new ScreenshotRecognizeResult()
        {
            MaskFilePath = outputFileMask,
            UpdatedScreenshotPath = outputFileResult
        };
    }

    private async Task Recognize(string filePath, string outputFile, string outputFileMask, IEnumerable<string> iconFiles, int maxItems)
    {
        try
        {
            var icons = await GetPredefinedItems(iconFiles);
            var originalScreenshot = new Mat(filePath);
            var screenshotMat = PrepareMat(filePath);

            var pointsInfo = await ExtractAllPositionsAsync(icons, screenshotMat);

            var limitedpointsInfo = pointsInfo
                .Take(maxItems)
                .ToArray();

            DrawSearchResultBorders(limitedpointsInfo, originalScreenshot);

            using (var fs = File.Create(outputFile))
            {
                originalScreenshot.WriteToStream(fs);
            }
            using (var fs = File.Create(outputFileMask))
            {
                screenshotMat.WriteToStream(fs);
            }

        }
        catch
        {
            throw;
        }
    }

    private Mat PrepareMat(string fileName)
    {
        var mat = new Mat(fileName, PREDEFINED_ImreadMode.Value);
        mat.ConvertTo(mat, PREDEFINED_MatType);
        //mat = mat.CvtColor(ColorConversionCodes.YUV420sp2RGBA);
        //mat = mat.Threshold(127, 255, ThresholdTypes.Binary);
        return mat;
    }

    private async Task<List<AbilityKnownLocation>> ExtractAllPositionsAsync(IEnumerable<AbilityMatWrapper> icons, Mat screenshotGray)
    {
        ConcurrentBag<AbilityKnownLocation> bag = new();

        var syncObj = new Object();
        int skippedCount = 0;

        var tasks = icons.Select(async (template) => await Task.Run(() =>
        {
            if (template.MatValue == null ||
            screenshotGray.Depth() != template.MatValue.Depth() ||
            screenshotGray.Type() != template.MatValue.Type())
            {
                lock (syncObj)
                {
                    skippedCount++;
                }
                return; // skip
            }

            var matchTemplate = screenshotGray.MatchTemplate(template.MatValue, PREDEFINED_TemplateMatchMode.Value);

            matchTemplate.MinMaxLoc(out double minVal, out double maxVal_, out var minLoc, out var maxLoc);

            //var allResults = matchTemplate.FindContoursAsArray(PREDEFINED_RetrievalMode.Value, PREDEFINED_ContourApproximationMode.Value);

            template.MatValue?.Dispose();
            matchTemplate?.Dispose();

            bag.Add(new AbilityKnownLocation()
            {
                Position = minLoc,
                Weight = minVal,
                Title = template.ImageName
            });
        }));

        await Task.WhenAll(tasks.ToArray());

        return bag
            .OrderBy(i => i.Weight)
            .ToList();
    }

    private static void DrawSearchResultBorders(IEnumerable<AbilityKnownLocation> items,
        Mat screenshotOriginal,
        bool drawTotal = true,
        bool drawNumber = true,
        bool drawList = true)
    {
        Point titleStartPoint = new(50, 200);
        int titleRowOffset = 15;
        int defaultIconSize = 55;
        int totalPoints = 0;

        int i = 0;
        foreach (var item in items)
        {
            string text = item.Title.FillOverflow(32);

            Rect borderRect = new(item.Position, new(defaultIconSize, defaultIconSize));
            Cv2.Rectangle(screenshotOriginal, borderRect, Scalar.Fuchsia, 2);

            if (drawNumber)
            {
                var numberLocation = new Point(item.Position.X + (defaultIconSize / 2), item.Position.Y + (defaultIconSize / 2));
                Cv2.PutText(screenshotOriginal, i.ToString(), numberLocation, HersheyFonts.HersheyPlain, 2, Scalar.Yellow);
            }
            if (drawList)
            {
                var labelLocation = new Point(titleStartPoint.X, titleStartPoint.Y + (i * titleRowOffset));
                Cv2.PutText(screenshotOriginal, $"{i}. {text}", labelLocation, HersheyFonts.HersheyPlain, 1, Scalar.CornflowerBlue);
            }
            i++;
            totalPoints++;
        }

        if (drawTotal)
        {
            Cv2.PutText(screenshotOriginal, totalPoints.ToString(), new Point(150, 150), HersheyFonts.HersheyPlain, 5, Scalar.Red);
        }
    }

    private async Task<AbilityMatWrapper[]> GetPredefinedItems(IEnumerable<string> iconFiles)
    {
        var list = new List<AbilityMatWrapper>();

        foreach (var file in iconFiles)
        {
            list.Add(new AbilityMatWrapper()
            {
                ImageName = Path.GetFileNameWithoutExtension(file),
                MatValue = PrepareMat(file)
            });
        }

        return list.ToArray();
    }
}

public class ScreenshotRecognizeResult
{
    public string UpdatedScreenshotPath { get; set; }
    public string MaskFilePath { get; set; }
}

internal class AbilityKnownLocation
{
    public Point Position { get; set; }
    public double Weight { get; set; }
    public string Title { get; set; }
}
