using OpenCvSharp;

namespace ImageRecognitionSurfLib.ImageMatcherProcessor;

public abstract class ImageMatcherProcessorBase
{
    private readonly TemplateMatchModes matchMode;

    public ImageMatcherProcessorBase(TemplateMatchModes matchMode)
    {
        this.matchMode = matchMode;
    }
    public abstract Task<AbilityKnownLocation> GetKnownLocation(Mat bigSourceImage, Mat smallPartImage, string smallPartName);

    protected virtual AbilityKnownLocation GetKnowLocationFromResult(Mat mat, Size smallPartImage, string title)
    {
        mat.MinMaxLoc(out double minVal, out double maxVal, out var minLoc, out var maxLoc);

        double weight = IsSqDiffSelected() ? minVal : maxVal;
        Point topLeft = IsSqDiffSelected() ? minLoc : maxLoc;
        Size size = new(smallPartImage.Width, smallPartImage.Height);
        Point bottomRight = new(topLeft.X + size.Width, topLeft.Y + size.Height);

        return new AbilityKnownLocation()
        {
            Position = topLeft,
            Size = size,
            Weight = weight,
            Title = title
        };
    }

    private bool IsSqDiffSelected()
    {
        return matchMode == TemplateMatchModes.SqDiff || matchMode == TemplateMatchModes.SqDiffNormed;
    }
}
