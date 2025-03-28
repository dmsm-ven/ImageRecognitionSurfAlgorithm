using ImageRecognitionSurfAlgorithm.ViewModels;
using ImageRecognitionSurfLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace ImageRecognitionSurfAlgorithm;

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
                services.AddSingleton<ImageRecognitionSurfProcessor>();
                services.AddSingleton<MainWindowViewModel>();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        MainWindow = new MainWindow();
        MainWindow.DataContext = AppHost.Services.GetService<MainWindowViewModel>();
        MainWindow.Show();
    }
}
