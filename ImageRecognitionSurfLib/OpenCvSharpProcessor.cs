using OpenCvSharp;

namespace ImageRecognitionSurfLib;
public class OpenCvSharpProcessor
{
    public double Threshold { get; set; } = 0.8;
    public double ThresholdMaxValue { get; set; } = 255;
    public ThresholdTypes? ThresholdType { get; set; } = null;
    public TemplateMatchModes? PREDEFINED_TemplateMatchMode { get; set; } = null;
    public ImreadModes? PREDEFINED_ImreadMode { get; set; } = null;
    public RetrievalModes? PREDEFINED_RetrievalMode { get; set; } = null;
    public ContourApproximationModes? PREDEFINED_ContourApproximationMode { get; set; } = null;
    public MatType PREDEFINED_MatType { get; set; } = MatType.CV_8U;

    public async Task<string> RecognizeDataToFile(string filePath, IEnumerable<string> iconFiles)
    {
        string outputFile = Path.Combine(Directory.GetCurrentDirectory(), "cache",
            Path.GetFileNameWithoutExtension(filePath) + "_result_" + DateTime.Now.Ticks + Path.GetExtension(filePath));

        string dir = Path.GetDirectoryName(outputFile);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await Recognize(filePath, outputFile, iconFiles);

        return outputFile;
    }

    private async Task Recognize(string filePath, string outputFilePath, IEnumerable<string> iconFiles)
    {
        try
        {
            var icons = await GetPredefinedItems(iconFiles);
            var screenshotOriginal = Cv2.ImRead(filePath, PREDEFINED_ImreadMode.Value);
            screenshotOriginal.ConvertTo(screenshotOriginal, PREDEFINED_MatType);

            var bordersDic = ExtractAllPositions(icons, screenshotOriginal);

            DrawSearchResultBorders(bordersDic, screenshotOriginal);

            using var fs = File.Create(outputFilePath);
            screenshotOriginal.WriteToStream(fs);
        }
        catch
        {
            throw;
        }
    }

    private Dictionary<AbilityMatWrapper, Point> ExtractAllPositions(IEnumerable<AbilityMatWrapper> icons, Mat screenshotGray)
    {
        Dictionary<AbilityMatWrapper, Point> bordersDic = new();

        foreach (var template in icons)
        {
            var matchTemplate = screenshotGray.MatchTemplate(template.MatValue, PREDEFINED_TemplateMatchMode.Value);
            matchTemplate.MinMaxLoc(out double _, out _, out var minLoc, out var _);

            bordersDic[template] = minLoc;
        }

        return bordersDic;
    }

    private static void DrawSearchResultBorders(Dictionary<AbilityMatWrapper, Point> bordersDic,
        Mat screenshotOriginal,
        bool drawTotal = true)
    {
        int titleOffset = -10;
        int defaultIconSize = 60;

        int totalPoints = 0;

        foreach (var kvp in bordersDic)
        {
            string text = kvp.Key.ImageName.FillOverflow();
            var point = kvp.Value;

            Rect borderRect = new(point, new(defaultIconSize, defaultIconSize));
            Cv2.Rectangle(screenshotOriginal, borderRect, Scalar.Fuchsia, 2);

            var textLocation = new Point(point.X, point.Y + titleOffset);
            Cv2.PutText(screenshotOriginal, text, textLocation, HersheyFonts.HersheyPlain, 1, Scalar.CornflowerBlue);

            totalPoints++;


            kvp.Key.MatValue?.Dispose();
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
            var mat = Cv2.ImRead(file, PREDEFINED_ImreadMode.Value);
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
