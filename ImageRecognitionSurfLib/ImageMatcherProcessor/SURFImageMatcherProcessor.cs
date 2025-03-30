using OpenCvSharp;
using OpenCvSharp.XFeatures2D;

namespace ImageRecognitionSurfLib.ImageMatcherProcessor;

public class SURFImageMatcherProcessor : ImageMatcherProcessorBase
{
    private readonly SURF surf = SURF.Create(hessianThreshold: 100);

    public SURFImageMatcherProcessor() : base(TemplateMatchModes.CCoeffNormed) { }

    public override async Task<AbilityKnownLocation?> GetKnownLocation(Mat mainImage, Mat subImage, string smallPartName)
    {
        // Step 2: Detect keypoints and compute descriptors
        KeyPoint[] keypoints1, keypoints2;
        surf.DetectAndCompute(mainImage, null, out keypoints1, mainImage);
        surf.DetectAndCompute(subImage, null, out keypoints2, subImage);

        if ((mainImage.GetType() != subImage.GetType()) || keypoints1.Length == 0 || keypoints2.Length == 0)
        {
            return null;
        }

        // Step 3: Match descriptors using BFMatcher
        //var bfMatcher = new BFMatcher(NormTypes.L2); // Use L2 norm for SURF
        using var bfMatcher = new BFMatcher(NormTypes.L2); // Use L2 norm for SURF
        var matches = bfMatcher.Match(mainImage, subImage);

        // Step 4: Filter matches (e.g., sort by distance and threshold)
        List<DMatch> goodMatches = new(matches);

        var srcPoints = goodMatches.Select(m => keypoints2[m.TrainIdx].Pt).ToArray();
        var dstPoints = goodMatches.Select(m => keypoints1[m.QueryIdx].Pt).ToArray();

        using Mat homography = Cv2.FindHomography(InputArray.Create(srcPoints), InputArray.Create(dstPoints), HomographyMethods.Ransac);

        //Фильтруем по дистанции
        float maxDistance = matches.Max(mt => mt.Distance);
        goodMatches = goodMatches
            .OrderBy(m => m.Distance) // Sort by distance
            .Where(m => m.Distance < 0.4 * maxDistance)
            .Take(8)
            .ToList();

        if (goodMatches.Count > 0)
        {
            var topMatch = goodMatches.First();
            var pt = keypoints1[topMatch.QueryIdx].Pt;
            var boundingBox = Cv2.BoundingRect(new[] { pt });
            var size = boundingBox.Size;
            double weightAvg = goodMatches.Average(i => i.Distance);

            var result = new AbilityKnownLocation()
            {
                Position = pt.ToPoint(),
                Weight = weightAvg,
                Title = smallPartName,
                Size = size
            };

            return result;
        }

        return null;
    }
}
