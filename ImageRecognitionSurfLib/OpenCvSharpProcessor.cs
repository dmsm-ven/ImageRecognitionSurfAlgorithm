using ImageRecognitionSurfLib.ImageMatcherProcessor;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace ImageRecognitionSurfLib;

public class OpenCvSharpProcessor
{
    public ImreadModes? PREDEFINED_ImreadMode { get; set; } = ImreadModes.Grayscale;
    public MatType PREDEFINED_MatType { get; set; } = MatType.CV_8U;
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

    public async Task<ScreenshotRecognizeResult> RecognizeDataToFile(string filePath, IEnumerable<string> iconFiles, int maxItems)
    {
        SurftRecognizer.HessianThreshold = SurftRecognizer_HessianThreshold;
        string ext = DateTime.Now.Ticks + Path.GetExtension(filePath);

        string outputFileResult = Path.Combine(Directory.GetCurrentDirectory(), "cache",
            Path.GetFileNameWithoutExtension(filePath) + $"_result_{ext}");

        string dir = Path.GetDirectoryName(outputFileResult);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await Recognize(filePath, outputFileResult, iconFiles, maxItems);

        return new ScreenshotRecognizeResult()
        {
            UpdatedScreenshotPath = outputFileResult
        };
    }

    private Mat BuildMat(string fileName, bool isMain)
    {
        var mat = new Mat(fileName, PREDEFINED_ImreadMode.Value);
        if (UseConvertToGrayOptions)
        {
            mat.CvtColor(ColorConversionCodes.GRAY2BGR);
        }
        if (UseThresholdOptions)
        {
            mat = mat.Threshold(Threshold_Thresh, Threshold_MaxVal, Threshold_Type);
        }
        if (UseCannyOptions)
        {
            mat = mat.Canny(Canny_Treshold1, Canny_Treshold2, Canny_AppertureSize, Canny_L2Gradient);
        }
        if (isMain)
        {
            mat = new Mat(mat, DeskSpellsPanelBounds);
        }
        return mat;
    }

    public async Task<string> RotateAgnosticCheck(string screenshotPath, string iconPath, int maxPoints)
    {
        var screenshotMat = BuildMat(screenshotPath, isMain: true);
        var iconMat = BuildMat(iconPath, isMain: false);

        string name = Path.Combine(Path.GetFileNameWithoutExtension(screenshotPath) + $"_result_{DateTime.Now.Ticks}" + Path.GetExtension(screenshotPath));
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "cache", name);

        var resultImage = await SurftRecognizer.DetectAndMatchFeaturesUsingSURF(screenshotMat, iconMat, Path.GetFileNameWithoutExtension(iconPath), maxPoints);

        resultImage.SaveImage(outputPath);

        return outputPath;
    }

    private async Task Recognize(string filePath, string outputFile, IEnumerable<string> iconFiles, int maxItems)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(outputFile) || iconFiles.Count() == 0 || maxItems == 0)
        {
            return;
        }

        try
        {
            var icons = await GetPredefinedItems(iconFiles);

            var screenshotMat = new Mat(filePath, ImreadModes.Color);
            screenshotMat.ConvertTo(screenshotMat, PREDEFINED_MatType);
            screenshotMat = new Mat(screenshotMat, DeskSpellsPanelBounds);

            var pointsInfo = await ExtractAllPositionsAsync(screenshotMat, icons, maxItems);

            screenshotMat.DrawSearchResultBorders(pointsInfo); // static helper метод

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
        await SurftRecognizer.WriteLogLine(JsonSerializer.Serialize(sortedByWeight));

        return sortedByWeight;
    }

    private async Task<AbilityMatWrapper[]> GetPredefinedItems(IEnumerable<string> iconFiles)
    {
        var list = new List<AbilityMatWrapper>();

        foreach (var file in iconFiles)
        {
            var mat = new Mat(file, ImreadModes.Color);
            mat.ConvertTo(mat, PREDEFINED_MatType);

            list.Add(new AbilityMatWrapper()
            {
                ImageName = Path.GetFileNameWithoutExtension(file),
                MatValue = mat
            });
        }

        return list.ToArray();
    }
}

internal static class SurftRecognizer
{
    public static int HessianThreshold { get; set; } = 100;
    private static JsonSerializerOptions options = new() { WriteIndented = false };

    private static string logFile = "log.txt";

    public static void OnlyRectangle(Mat mainImage)
    {
        Point[][] contours;
        HierarchyIndex[] hierarchy;
        mainImage.FindContours(out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

        foreach (var contour in contours)
        {
            double epsilon = 0.02 * Cv2.ArcLength(contour, true);
            Point[] approx = Cv2.ApproxPolyDP(contour, epsilon, true);

            if (approx.Length == 4 && Cv2.IsContourConvex(approx))
            {
                // Calculate aspect ratio
                var boundingRect = Cv2.BoundingRect(approx);
                float aspectRatio = (float)boundingRect.Width / boundingRect.Height;

                // Check if aspect ratio is close to 1 and angles are 90 degrees
                if (Math.Abs(aspectRatio - 1) < 0.1)
                {
                    // This is likely a square
                    Cv2.DrawContours(mainImage, new[] { approx }, -1, Scalar.Red, 2);
                }
            }
        }

        Cv2.ImShow("Detected Squares", mainImage);
        Cv2.WaitKey();
    }

    public static async Task<Mat> DetectAndMatchFeaturesUsingSURF(Mat mainImage, Mat subImage, string iconName, int maxPoints)
    {
        // Step 1: Create SURF detector
        var surf = SURF.Create(HessianThreshold);

        // Step 2: Detect keypoints and compute descriptors
        KeyPoint[] keypoints1, keypoints2;
        Mat descriptors1 = new(), descriptors2 = new();

        surf.DetectAndCompute(mainImage, null, out keypoints1, descriptors1);
        surf.DetectAndCompute(subImage, null, out keypoints2, descriptors2);

        // Step 3: Match descriptors using BFMatcher
        //var bfMatcher = new BFMatcher(NormTypes.L2); // Use L2 norm for SURF
        var bfMatcher = new BFMatcher(NormTypes.L2); // Use L2 norm for SURF
        var matches = bfMatcher.Match(descriptors1, descriptors2);

        // Step 4: Filter matches (e.g., sort by distance and threshold)
        List<DMatch> goodMatches = new(matches);

        var srcPoints = goodMatches.Select(m => keypoints2[m.TrainIdx].Pt).ToArray();
        var dstPoints = goodMatches.Select(m => keypoints1[m.QueryIdx].Pt).ToArray();

        if (srcPoints.Length == 0 || dstPoints.Length == 0)
        {
            return mainImage;
        }

        Mat homography = Cv2.FindHomography(InputArray.Create(srcPoints), InputArray.Create(dstPoints), HomographyMethods.Ransac);

        //Фильтруем по дистанции
        float maxDistance = matches.Max(mt => mt.Distance);
        goodMatches = goodMatches
            .OrderBy(m => m.Distance) // Sort by distance
            .Where(m => m.Distance < 0.35)
            .Take(maxPoints)
            .ToList();

        // Step 5: Visualize matches (Optional)
        Mat matchOutput = new();

        Cv2.DrawMatches(mainImage, keypoints1, subImage, keypoints2, goodMatches, matchOutput,
            Scalar.Fuchsia,
            Scalar.Fuchsia,
            null,
            DrawMatchesFlags.DrawRichKeypoints | DrawMatchesFlags.NotDrawSinglePoints);

        await WriteLogLine(goodMatches, iconName);

        // Display matches

        return matchOutput;
    }

    public static async Task WriteLogLine(string message)
    {
        await File.AppendAllLinesAsync(logFile, new string[] { message });
    }
    public static async Task WriteLogLine(IEnumerable<DMatch> matches, string iconName)
    {

        await File.AppendAllLinesAsync(logFile, new string[] { iconName });
        if (matches is not null)
        {
            var lines = matches
                .Select(i => new MatchLogWrapper(i.TrainIdx, i.Distance.ToString("F2", new CultureInfo("en-EN"))))
                .Select(m => JsonSerializer.Serialize(m, options)).ToArray();
            await File.AppendAllLinesAsync(logFile, lines);
        }
    }
}

internal enum HessianThresholdMode
{
    Regular = 200,
    Balanced = 400,
    High = 800
}

internal record MatchLogWrapper(decimal TrainIdx, string Distance);