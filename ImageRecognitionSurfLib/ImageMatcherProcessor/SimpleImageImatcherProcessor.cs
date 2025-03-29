using OpenCvSharp;

namespace ImageRecognitionSurfLib.ImageMatcherProcessor;
public class SimpleImageImatcherProcessor : ImageMatcherProcessorBase
{
    private readonly TemplateMatchModes matchMode;

    public SimpleImageImatcherProcessor(TemplateMatchModes matchMode) : base(matchMode)
    {
        this.matchMode = matchMode;
    }

    public override async Task<AbilityKnownLocation> GetKnownLocation(Mat bigSourceImage, Mat smallPartImage, string smallPartName)
    {
        var matchTemplate = await Task.Run(() => bigSourceImage.MatchTemplate(smallPartImage, matchMode));

        return base.GetKnowLocationFromResult(matchTemplate, smallPartImage.Size(), smallPartName);
    }

}
