using OpenCvSharp;
namespace ImageRecognitionSurfLib;

public class AbilityKnownLocation
{
    public Point Position { get; set; }
    public Size Size { get; set; }
    public Rect Rect => new(Position, Size);
    public double Weight { get; set; }
    public string Title { get; set; }
}
