using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Models;

namespace FactoryPlanner.Services;

public static class Files
{
    //PickOpen = opens a file picker dialog and returns the selected file path
    public static Func<string, Task<string?>> PickOpen { get; set; } = _ => Task.FromResult<string?>(null);
    
    //PickSave = opens a file save dialog and returns the selected file path
    public static Func<string, string, Task<string?>> PickSave { get; set; } = (_, _) => Task.FromResult<string?>(null);
    
    //Report = reports a message to the user
    public static Action<string> Report { get; set; } = _ => { };
}

public static class Json
{
    //Options = options for JSON serialization and deserialization
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { DropCommands } }
    };

    //DropCommands = removes properties of type ICommand from the JSON serialization and deserialization
    private static void DropCommands(JsonTypeInfo info)
    {
        var commands = info.Properties.Where(p => typeof(ICommand).IsAssignableFrom(p.PropertyType)).ToList();
        foreach (var command in commands)
            info.Properties.Remove(command);
    }
}

//ICatalog = interface for a catalog of items, structures, or recipes
public interface ICatalog
{
    string Title { get; }
    void Export(string path);
    int Import(string path);
}

//Catalog<T> = a catalog of items, structures, or recipes
// partial class = allows the class to be split across multiple files
public partial class Catalog<T> : ObservableObject, ICatalog where T : ObservableObject, INamed
{
    private const int PageSize = 48;

    private readonly Func<T> create;
    private readonly Func<T, string, bool> matches;
    private int filtered;

    //noun = item/recipe/structure
    public Catalog(string title, string noun, Func<T> create, Func<T, string, bool> matches)
    {
        Title = title;
        AddLabel = "Add " + noun;
        SearchHint = "Search " + title.ToLower();
        this.create = create;
        this.matches = matches;
    }

    public string Title { get; }
    public string AddLabel { get; }
    public string SearchHint { get; }
    public ObservableCollection<T> All { get; } = new();
    public ObservableCollection<T> View { get; } = new();


    [ObservableProperty] private string search = "";
    [ObservableProperty] private T? selected;
    [ObservableProperty] private int page;
    [ObservableProperty] private string summary = "";
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private bool hasPages;

    //OnSearchChanged = called when the Search changes
    //partial void = allows the method to be implemented in another part of the partial class
    partial void OnSearchChanged(string value)
    {
        if (Page != 0) Page = 0;
        else Refresh();
    }

    //OnPageChanged = called when the Page changes
    partial void OnPageChanged(int value) => Refresh();

    //OnSelectedChanged = called when the Selected changes
    partial void OnSelectedChanged(T? value) => DeleteCommand.NotifyCanExecuteChanged();

    //Add = adds a new item, structure, or recipe to the catalog
    [RelayCommand]
    private void Add()
    {
        Search = "";
        var fresh = create();
        All.Add(fresh);
        var index = Sorted().IndexOf(fresh);
        var target = Math.Max(0, index) / PageSize;
        if (target != Page) Page = target;
        else Refresh();
        Selected = fresh;
    }

    //Delete = removes the selected item, structure, or recipe from the catalog
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        if (Selected is not { } doomed) return;
        All.Remove(doomed);
        Selected = null;
        Refresh();
    }

    //Close = clears the selection
    [RelayCommand]
    private void Close() => Selected = null;

    //Prev = goes to the previous page of the catalog
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Prev() => Page--;

    //Next = goes to the next page of the catalog
    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void Next() => Page++;

    //async Task = represents an asynchronous operation that can be awaited
    //ExportAsync = exports the catalog to a JSON file
    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = await Files.PickSave("Export " + Title.ToLower(), Title.ToLower() + ".json");
        if (path is null) return;
        try
        {
            Export(path);
            Files.Report($"Exported {All.Count} {Title.ToLower()} to {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            Files.Report("Export failed: " + ex.Message);
        }
    }

    //ImportAsync = imports items from a JSON file
    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = await Files.PickOpen("Import " + Title.ToLower());
        if (path is null) return;
        try
        {
            var count = Import(path);
            Files.Report($"Imported {count} {Title.ToLower()} from {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            Files.Report("Import failed: " + ex.Message);
        }
    }

    //Export = exports the catalog to a JSON file
    public void Export(string path) => File.WriteAllText(path, JsonSerializer.Serialize(All, Json.Options));

    //Import = imports items from a JSON file
    public int Import(string path)
    {
        var incoming = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), Json.Options) ?? new();
        foreach (var entry in incoming)
        {
            var existing = All.FirstOrDefault(x => string.Equals(x.Name, entry.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null) All.Remove(existing);
            All.Add(entry);
        }
        Selected = null;
        Refresh();
        return incoming.Count;
    }

    //ReplaceAll = replaces all items in the catalog with the incoming items
    public void ReplaceAll(IEnumerable<T> incoming)
    {
        All.Clear();
        foreach (var entry in incoming) All.Add(entry);
        Selected = null;
        Refresh();
    }


    private bool HasSelection() => Selected is not null;
    private bool CanGoBack() => Page > 0;
    private bool CanGoForward() => (Page + 1) * PageSize < filtered;

    //Sorted = returns a sorted list of items, structures, or recipes that match the search term
    private List<T> Sorted(string term = "") =>
        All.Where(x => term.Length == 0 || matches(x, term)).OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    //Refresh = refreshes the view of the catalog based on the current search term and page
    private void Refresh()
    {
        var keep = Selected;
        var term = Search.Trim();
        var hits = Sorted(term);
        filtered = hits.Count;

        var pages = Math.Max(1, (filtered + PageSize - 1) / PageSize);
        if (Page >= pages)
        {
            Page = pages - 1;
            return;
        }

        View.Clear();
        foreach (var hit in hits.Skip(Page * PageSize).Take(PageSize))
            View.Add(hit);
        Selected = keep is not null && View.Contains(keep) ? keep : null;

        HasPages = pages > 1;
        IsEmpty = filtered == 0;
        Summary = term.Length == 0 ? $"{filtered} {Title.ToLower()}" : $"{filtered} of {All.Count}";
        if (HasPages) Summary += $"   {Page + 1}/{pages}";

        PrevCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }
}
