
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
        using var src = new Mat(filePath, ImreadModes.Grayscale);
        using var dst = new Mat();

        Cv2.Canny(src, dst, 100, 200);

        using var fs = File.Create(outputFilePath);
        dst.WriteToStream(fs);
    }
}
