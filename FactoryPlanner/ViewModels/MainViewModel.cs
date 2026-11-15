using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Models;
using FactoryPlanner.Services;

namespace FactoryPlanner.ViewModels;

public class Workspace
{
    public List<Item> Items { get; set; } = new();
    public List<Structure> Structures { get; set; } = new();
    public List<Recipe> Recipes { get; set; } = new();
}

//View model = contains the data and logic for the UI
public partial class MainViewModel : ObservableObject
{
    //AutoSavePath = path to the auto save file
    public static string AutoSavePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FactoryPlanner", "workspace.json");

    //Catalog = collection of items, structures, and recipes
    public Catalog<Item> Items { get; } = new("Items", "item", () => new Item(), (i, t) => Has(i.Name, t) || Has(i.Category, t));

    public Catalog<Structure> Structures { get; } = new("Structures", "structure", () => new Structure(), (s, t) => Has(s.Name, t) || Has("tier " + s.Tier, t));

    public Catalog<Recipe> Recipes { get; } = new("Recipes", "recipe", () => new Recipe(), (r, t) => Has(r.Name, t) || Has(r.OutputItem, t) || Has(r.Machine, t) || r.Inputs.Any(x => Has(x.ItemName, t)));

    [ObservableProperty]
    private string status = "Ready";

    public MainViewModel()
    {
        Files.Report = message => Status = message;

        if (!TryRestore())
            Seed();
    }

    public void Save(string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
            Directory.CreateDirectory(folder);

        var workspace = new Workspace
        {
            Items = Items.All.ToList(),
            Structures = Structures.All.ToList(),
            Recipes = Recipes.All.ToList()
        };

        File.WriteAllText(path, JsonSerializer.Serialize(workspace, Json.Options));
    }

    public void Load(string path)
    {
        Workspace workspace = JsonSerializer.Deserialize<Workspace>(File.ReadAllText(path), Json.Options) ?? new Workspace();

        Items.ReplaceAll(workspace.Items);
        Structures.ReplaceAll(workspace.Structures);
        Recipes.ReplaceAll(workspace.Recipes);
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        var path = await Files.PickOpen("Open project");

        if (path is null)
            return;

        try
        {
            Load(path);
            Status = "Opened " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            Status = "Open failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveProject()
    {
        var path = await Files.PickSave("Save project", "factory.json");

        if (path is null)
            return;

        try
        {
            Save(path);
            Status = "Saved " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            Status = "Save failed: " + ex.Message;
        }
    }

    private bool TryRestore()
    {
        try
        {
            if (!File.Exists(AutoSavePath))
                return false;

            Load(AutoSavePath);
            Status = "Restored last session";

            return true;
        }
        catch
        {
            return false;
        }
    }

    //Seed = adds some default items, structures, and recipes to the catalog
    private void Seed()
    {
        Items.ReplaceAll(new[]
        {
            new Item
            {
                Name = "Iron Ore",
                Category = "Raw",
                StackSize = 100
            },
            new Item
            {
                Name = "Iron Ingot",
                Category = "Metal",
                StackSize = 100
            }
        });

        Structures.ReplaceAll(new[]
        {
            new Structure
            {
                Name = "Smelter",
                PowerMw = 4,
                Width = 6,
                Length = 9,
                Tier = 1
            }
        });

        Recipes.ReplaceAll(new[]
        {
            MakeRecipe(
                "Iron Ingot",
                "Iron Ingot",
                30,
                "Smelter",
                ("Iron Ore", 30))
        });
    }

    private static Recipe MakeRecipe(string name, string output, decimal rate, string machine, params (string Item, decimal Amount)[] inputs) => new()
        {
            Name = name,
            OutputItem = output,
            OutputPerMin = rate,
            Machine = machine,
            Inputs = new(
                inputs.Select(i => new RecipeInput
                {
                    ItemName = i.Item,
                    Amount = i.Amount
                }))
        };

    private static bool Has(string? text, string term) =>
        text?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true;
}