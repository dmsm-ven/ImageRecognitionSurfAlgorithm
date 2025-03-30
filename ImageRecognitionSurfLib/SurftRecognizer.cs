using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using System.Globalization;
using System.Text.Json;

namespace ImageRecognitionSurfLib;

internal static class SurftRecognizer
{
    public static int HessianThreshold { get; set; } = 100;
    public static NormTypes NormType { get; set; } = NormTypes.L2;

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
        var bfMatcher = new BFMatcher(NormType); // Use L2 norm for SURF
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
