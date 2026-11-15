using System;
using System.IO;
using System.Threading.Tasks;
using AsyncImageLoader;
using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FactoryPlanner.Services;
using FactoryPlanner.ViewModels;

namespace FactoryPlanner.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType JsonFiles = new("JSON files") { Patterns = new[] { "*.json" } };

    public MainWindow()
    {
        InitializeComponent();
        ClampToScreen();

        var dataFolder = Path.GetDirectoryName(MainViewModel.AutoSavePath)!;
        ImageLoader.AsyncImageLoader = new DiskCachedWebImageLoader(Path.Combine(dataFolder, "images"));

        Files.PickOpen = PickOpen;
        Files.PickSave = PickSave;

        var vm = new MainViewModel();
        DataContext = vm;

        Closing += (_, _) =>
        {
            try { vm.Save(MainViewModel.AutoSavePath); }
            catch { }
        };
    }

    //ClampToScreen = ensures the window is within the screen bounds, doesnt work?
    private void ClampToScreen()
    {
        if (Screens.Primary is not { } screen) return;

        var area = screen.WorkingArea;
        Width = Math.Min(Width, area.Width / screen.Scaling - 16);
        Height = Math.Min(Height, area.Height / screen.Scaling - 56);
        MinWidth = Math.Min(MinWidth, Width);
        MinHeight = Math.Min(MinHeight, Height);
    }

    //PickOpen = opens a file picker dialog and returns the selected file path
    private async Task<string?> PickOpen(string title)
    {
        var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[] { JsonFiles }
        });
        return picked.Count > 0 ? picked[0].TryGetLocalPath() : null;
    }

    //PickSave = opens a file save dialog and returns the selected file path
    private async Task<string?> PickSave(string title, string fileName)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = fileName,
            DefaultExtension = "json",
            FileTypeChoices = new[] { JsonFiles }
        });
        return file?.TryGetLocalPath();
    }
}
