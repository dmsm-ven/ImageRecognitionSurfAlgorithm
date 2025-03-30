using System.IO;
using System.Text.Json;

namespace ImageRecognitionSurfUI;

public interface ISettingsStorage
{
    T GetByKey<T>(string key);
    void SaveAll(Dictionary<string, object> data);
}
public class JsonFileSettingsStorage : ISettingsStorage
{
    private readonly string fileName = "settings.json";
    private readonly JsonSerializerOptions options = new() { WriteIndented = true };

    public T GetByKey<T>(string key)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(fileName), options);

        Type typeParameterType = typeof(T);

        if (data != null && data.ContainsKey(key))
        {
            if (typeParameterType == typeof(double))
            {
                return (T)(object)double.Parse(data[key].ToString().Replace(".", ","));
            }
            else if (typeParameterType == typeof(int))
            {
                return (T)(object)int.Parse(data[key].ToString());
            }
            else if (typeParameterType == typeof(bool))
            {
                return (T)(object)bool.Parse(data[key].ToString());
            }
            else if (typeParameterType == typeof(string))
            {
                return (T)(object)data[key].ToString();
            }
        }

        throw new KeyNotFoundException($"Key '{key}' not found in settings.");
    }

    public void SaveAll(Dictionary<string, object> data)
    {
        string jsoNText = JsonSerializer.Serialize(data, options);
        File.WriteAllText(fileName, jsoNText);
    }
}
