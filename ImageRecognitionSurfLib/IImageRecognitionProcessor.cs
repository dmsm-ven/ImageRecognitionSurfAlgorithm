namespace ImageRecognitionSurfLib;

public interface IImageRecognitionProcessor
{
    Task<string> RecognizeDataToFile(string filePath);
}
