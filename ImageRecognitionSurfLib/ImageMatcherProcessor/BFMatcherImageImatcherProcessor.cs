using OpenCvSharp;

namespace ImageRecognitionSurfLib.ImageMatcherProcessor;

public class BFMatcherImageImatcherProcessor : ImageMatcherProcessorBase
{
    private readonly NormTypes normType;
    private readonly HomographyMethods homographyMethod;
    private readonly Dictionary<Mat, KeyPoint[]> keyPointsMap;

    public BFMatcherImageImatcherProcessor(Dictionary<Mat, KeyPoint[]> keyPointsMap,
        TemplateMatchModes templateMatchModes,
        NormTypes normType = NormTypes.Hamming2,
        HomographyMethods homographyMethod = HomographyMethods.Ransac) : base(templateMatchModes)
    {
        this.normType = normType;
        this.homographyMethod = homographyMethod;
        this.keyPointsMap = keyPointsMap;
    }

    public override async Task<AbilityKnownLocation> GetKnownLocation(Mat bigSourceImage, Mat smallPartImage, string smallPartName)
    {
        Mat homography = await Task.Run(() =>
        {

            var bfMatcher = new BFMatcher(normType);
            var matches = bfMatcher.Match(bigSourceImage, smallPartImage);

            var srcPoints = matches.Select(m => keyPointsMap[smallPartImage][m.TrainIdx].Pt).ToArray();
            var dstPoints = matches.Select(m => keyPointsMap[bigSourceImage][m.QueryIdx].Pt).ToArray();

            var srcArray = InputArray.Create(srcPoints);
            var dstArray = InputArray.Create(dstPoints);

            return Cv2.FindHomography(srcArray, dstArray, homographyMethod);
        });

        var knownLocation = GetKnowLocationFromResult(homography, smallPartImage.Size(), smallPartName);


        return knownLocation;
    }

    protected override AbilityKnownLocation GetKnowLocationFromResult(Mat mat, Size smallPartImage, string title)
    {
        return base.GetKnowLocationFromResult(mat, smallPartImage, title);
    }

}
