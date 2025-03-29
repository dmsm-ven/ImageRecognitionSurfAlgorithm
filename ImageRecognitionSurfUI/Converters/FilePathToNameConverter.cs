using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace ImageRecognitionSurfUI.Converters;

public class FilePathToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return "<NULL>";
        }

        try
        {
            var fi = new FileInfo(value.ToString());
            if (fi.Exists)
            {
                return fi.Name;
            }
        }
        catch
        {

        }

        return "<Файл не найден>";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
