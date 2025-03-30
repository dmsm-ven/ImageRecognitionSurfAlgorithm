using ImageRecognitionSurfLib;
using ImageRecognitionSurfUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;

namespace ImageRecognitionSurfUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost AppHost;
    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ISettingsStorage, JsonFileSettingsStorage>();
                services.AddSingleton<OpenCvSharpProcessor>();
                services.AddSingleton<MainWindowViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await ClearCacheFolder();

        MainWindow = new MainWindow();
        MainWindow.DataContext = AppHost.Services.GetService<MainWindowViewModel>();
        MainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await ClearCacheFolder();
    }


    public static async Task ClearCacheFolder()
    {
        string cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "cache");
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }

        int retryCount = 0;
        int retryMax = 5;
        TimeSpan delay = TimeSpan.FromSeconds(1);
        bool isDone = false;

        do
        {
            try
            {
                var cacheFiles = Directory.GetFiles(cacheDir).ToList();
                if (cacheFiles.Any())
                {
                    cacheFiles.ForEach(File.Delete);
                }
                isDone = true;
            }
            catch
            {
                retryCount++;
            }
            if (!isDone)
            {
                await Task.Delay(delay);
            }
        } while (!isDone && retryCount <= retryMax);
    }
}
