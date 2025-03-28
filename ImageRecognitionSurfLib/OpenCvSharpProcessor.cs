
using OpenCvSharp;

namespace ImageRecognitionSurfLib;
public class OpenCvSharpProcessor : IImageRecognitionProcessor
{
    public async Task<string> RecognizeDataToFile(string filePath)
    {
        string outputFile = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "_result" + Path.GetExtension(filePath));

        await Recognize(filePath, outputFile);

        return outputFile;
    }

    private async Task Recognize(string filePath, string outputFilePath)
    {
        var icons = await GetPredefinedItems();
        var template = icons.First(); // alchemist_chemical_rage.png
        var screenshotOriginal = Cv2.ImRead(filePath);
        screenshotOriginal.ConvertTo(screenshotOriginal, MatType.CV_8UC1);
        var screenshotGray = screenshotOriginal.CvtColor(ColorConversionCodes.BGR2GRAY);
        var matchTemplate = screenshotGray.MatchTemplate(template.MatValue, TemplateMatchModes.CCoeffNormed);

        Mat matchTemplateConverted = new();
        if (matchTemplate.Type() != MatType.CV_8UC1)
        {
            matchTemplate.ConvertTo(matchTemplateConverted, MatType.CV_8UC1);
        }

        var tresholdResults = matchTemplateConverted.Threshold(0.8, 256, ThresholdTypes.Otsu);

        DrawSearchResultBorders(template, screenshotOriginal, tresholdResults);

        using var fs = File.Create(outputFilePath);
        screenshotOriginal.WriteToStream(fs);
    }

    private static void DrawSearchResultBorders(AbilityMatWrapper template, Mat screenshotOriginal, Mat tresholdResults)
    {
        var contours = tresholdResults.FindContoursAsArray(RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

        // Draw rectangles around detected areas
        foreach (var contour in contours)
        {
            var rect = Cv2.BoundingRect(contour);

            Rect borderRect = new(rect.Location, new Size(60, 60));
            Cv2.Rectangle(screenshotOriginal, borderRect, Scalar.Fuchsia, 2);

            Point textLocation = new(rect.Location.X, rect.Location.Y - 10);
            string text = template.ImageName.Take(16).ToString();
            Cv2.PutText(screenshotOriginal, text, textLocation, HersheyFonts.HersheyPlain, 0.6, Scalar.CornflowerBlue);
        }
    }

    private async Task<AbilityMatWrapper[]> GetPredefinedItems()
    {
        var list = new List<AbilityMatWrapper>();
        string[] testIcons = Directory.GetFiles(@"C:\Users\user\Desktop\AI RES\ADDA\icons", "*.*", SearchOption.TopDirectoryOnly);

        foreach (var file in testIcons)
        {
            var mat = Cv2.ImRead(file, ImreadModes.Grayscale);
            Mat matConverted = new();
            mat.ConvertTo(matConverted, MatType.CV_8UC1);

            list.Add(new AbilityMatWrapper()
            {
                ImageName = Path.GetFileNameWithoutExtension(file),
                MatValue = matConverted
            });
        }

        return list.ToArray();
    }

    private class AbilityMatWrapper
    {
        public Mat MatValue { get; set; }
        public string ImageName { get; set; }
    }
}
