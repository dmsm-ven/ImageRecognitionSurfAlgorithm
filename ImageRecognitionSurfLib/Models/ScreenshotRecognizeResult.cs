namespace ImageRecognitionSurfLib.Models;

public class ScreenshotRecognizeResult
{
    public string UpdatedScreenshotPath { get; set; }
    public bool HasErrors { get; set; }
    public string ErrorMessage { get; set; }
}
