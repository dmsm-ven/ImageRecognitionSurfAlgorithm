using CommunityToolkit.Mvvm.ComponentModel;
using ImageRecognitionSurfLib;
using System.Collections.ObjectModel;
using System.IO;

namespace ImageRecognitionSurfUI.ViewModels;

public partial class ProcessOptionsViewModel : ObservableObject
{
    private readonly MainWindowViewModel parentVm;
    private readonly OpenCvSharpProcessor recProcessor;

    [ObservableProperty]
    private FileInfo? selectedIconFile = null;

    [ObservableProperty]
    private ObservableCollection<FileInfo> iconFiles = new();

    public ProcessOptionsViewModel(MainWindowViewModel parentVm)
    {
        this.parentVm = parentVm;
        this.PropertyChanged += MainWindowViewModel_PropertyChanged;

        Directory
            .GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "icons"))
            .Select(file => new FileInfo(file))
            .OrderBy(file => file.Name)
            .ToList().ForEach(file =>
        {
            IconFiles.Add(file);
        });
        SelectedIconFile = iconFiles.First();
    }

    private async void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // await parentVm.ProcessImageCommand.ExecuteAsync(null);
    }
}