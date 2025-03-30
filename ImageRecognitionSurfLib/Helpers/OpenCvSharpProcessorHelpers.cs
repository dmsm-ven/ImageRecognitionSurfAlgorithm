using OpenCvSharp;

namespace ImageRecognitionSurfLib.Helpers;

public static class OpenCvSharpProcessorHelpers
{
    public static void DrawSearchResultBorders(this Mat canvas,
        IEnumerable<AbilityKnownLocation> items,
    bool drawTotal = true,
    bool drawNumber = true,
    bool drawList = true)
    {
        Point titleStartPoint = new(50, 200);
        int titleRowOffset = 15;
        int totalPoints = 0;

        int i = 0;
        foreach (var item in items)
        {
            string text = item.Title.FillOverflow(32);

            Cv2.Rectangle(canvas, item.Rect, Scalar.Fuchsia, 2);


            if (drawNumber)
            {
                var numberLocation = new Point(item.Position.X + item.Size.Width / 2 - 15, item.Position.Y + 10 + item.Size.Height / 2);
                Cv2.PutText(canvas, i.ToString(), numberLocation, HersheyFonts.HersheyDuplex, 1.25, Scalar.Black, 2, LineTypes.AntiAlias);
            }
            if (drawList)
            {
                var labelLocation = new Point(titleStartPoint.X, titleStartPoint.Y + i * titleRowOffset);
                Cv2.PutText(canvas, $"{i}. {text}", labelLocation, HersheyFonts.HersheyPlain, 0.9, Scalar.Fuchsia);
            }
            i++;
            totalPoints++;
        }

        if (drawTotal)
        {
            Cv2.PutText(canvas, totalPoints.ToString(), new Point(150, 150), HersheyFonts.HersheyPlain, 5, Scalar.Red);
        }
    }
}