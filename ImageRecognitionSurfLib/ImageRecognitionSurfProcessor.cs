namespace ImageRecognitionSurfLib;

public class ImageRecognitionSurfProcessor
{
    public async Task<string> RecognizeDataToFile(string filePath)
    {
        await Task.Delay(TimeSpan.FromSeconds(3));

        return filePath;
    }
}
