using OpenCvSharp;

namespace ImageRecognitionSurfLib.ImageMatcherProcessor;
public static class ORBExample
{
    public static void ShowOrbExample(string filePath, string iconpath)
    {
        // Load two images (query image and train image)
        Mat queryImage = Cv2.ImRead(filePath, ImreadModes.Color);
        Mat trainImage = Cv2.ImRead(iconpath, ImreadModes.Color);
        queryImage.ConvertTo(queryImage, MatType.CV_8UC1);
        trainImage.ConvertTo(trainImage, MatType.CV_8UC1);

        // Create an ORB detector
        var orb = ORB.Create(500, 2.25f);

        // Detect keypoints and compute descriptors for both images
        KeyPoint[] keypointsQuery, keypointsTrain;
        Mat descriptorsQuery = new();
        Mat descriptorsTrain = new();
        orb.DetectAndCompute(queryImage, null, out keypointsQuery, descriptorsQuery);
        orb.DetectAndCompute(trainImage, null, out keypointsTrain, descriptorsTrain);

        // Use a brute-force matcher to match descriptors
        var bfMatcher = new BFMatcher(NormTypes.Hamming);
        var matches = bfMatcher.Match(descriptorsQuery, descriptorsTrain);

        // Sort matches by distance (best matches first)
        var goodMatches = matches.OrderBy(m => m.Distance).Take(3).ToArray();

        // Draw matches
        Mat resultImage = new();
        Cv2.DrawMatches(queryImage, keypointsQuery, trainImage, keypointsTrain, goodMatches, resultImage);

        // Show result
        Cv2.ImShow("ORB Keypoint Matches", resultImage);
        Cv2.WaitKey();
    }
}