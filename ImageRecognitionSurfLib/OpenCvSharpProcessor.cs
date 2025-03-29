using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
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

    public void RotateAgnosticCheck(string screenshotPath, string iconPath)
    {
        var screenshotMat = new Mat(screenshotPath, ImreadModes.Grayscale);
        var iconMat = new Mat(iconPath, ImreadModes.Grayscale);

        SurftRecognizer.DetectAndMatchFeaturesUsingSURF(screenshotMat, iconMat);
    }

    private async Task Recognize(string filePath, string outputFile, string outputFileMask, IEnumerable<string> iconFiles, int maxItems)
    {
        try
        {
            var icons = await GetPredefinedItems(iconFiles);
            var originalScreenshot = new Mat(filePath);
            var screenshotMat = PrepareMat(filePath);

            var pointsInfo = await ExtractAllPositionsAsync(icons, screenshotMat, maxItems);

            originalScreenshot.DrawSearchResultBorders(pointsInfo); // static helper метод

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
        return mat;
    }

    private async Task<List<AbilityKnownLocation>> ExtractAllPositionsAsync(IEnumerable<AbilityMatWrapper> icons, Mat screenshotGray, int maxItems)
    {
        //var matcher = new BFMatcherImageImatcherProcessor(keypointsDictionary, PREDEFINED_TemplateMatchMode.Value);
        var matcher = new SimpleImageImatcherProcessor(PREDEFINED_TemplateMatchMode.Value);

        ConcurrentBag<AbilityKnownLocation> bag = new();

        var syncObj = new Object();
        int skippedCount = 0;

        var tasks = icons.Select(async (template) => await Task.Run(async () =>
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

            var knownLocation = await matcher.GetKnownLocation(screenshotGray, template.MatValue, template.ImageName);

            bag.Add(knownLocation);

            template.MatValue?.Dispose();

        }));

        await Task.WhenAll(tasks.ToArray());

        /*List<AbilityKnownLocation> locations = IsSqDiffSelected() ?
            bag.Where(a => a.Weight <= 0.5).OrderBy(a => a.Weight).Take(maxItems).ToList() :
            bag.Where(a => a.Weight >= 0.5).OrderByDescending(a => a.Weight).Take(maxItems).ToList();
        */

        return bag.ToList();
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

public static class SurftRecognizer
{
    private enum HessianThresholdMode
    {
        Regular = 200,
        Balanced = 400,
        High = 800
    }

    public static void DetectAndMatchFeaturesUsingSURF(Mat mainImage, Mat subImage)
    {

        // Step 1: Create SURF detector
        var surf = SURF.Create(hessianThreshold: (int)HessianThresholdMode.High);

        // Step 2: Detect keypoints and compute descriptors
        KeyPoint[] keypoints1, keypoints2;
        Mat descriptors1 = new(), descriptors2 = new();
        surf.DetectAndCompute(mainImage, null, out keypoints1, descriptors1);
        surf.DetectAndCompute(subImage, null, out keypoints2, descriptors2);

        // Step 3: Match descriptors using BFMatcher
        var bfMatcher = new BFMatcher(NormTypes.L2); // Use L2 norm for SURF
        var matches = bfMatcher.Match(descriptors1, descriptors2);

        // Step 4: Filter matches (e.g., sort by distance and threshold)
        float max = matches.Max(mt => mt.Distance);
        var goodMatches = matches
            .OrderBy(m => m.Distance) // Sort by distance
            .Take(3)
            .ToList();

        // Step 5: Visualize matches (Optional)
        Mat matchOutput = new();
        Cv2.DrawMatches(mainImage, keypoints1, subImage, keypoints2, goodMatches, matchOutput,
            Scalar.Blue,
            Scalar.Fuchsia,
            null,
            DrawMatchesFlags.NotDrawSinglePoints | DrawMatchesFlags.DrawRichKeypoints);

        // Display matches
        Cv2.ImShow("Good Matches", matchOutput);
        Cv2.WaitKey();
    }
}