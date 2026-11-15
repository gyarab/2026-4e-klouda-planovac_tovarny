namespace FactoryPlanner.Models;
using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class Recipe : ObservableObject, INamed
{
    [ObservableProperty] private string name = "New recipe";
    [ObservableProperty] private string outputItem = "";
    [ObservableProperty] private decimal outputPerMin = 1;
    [ObservableProperty] private string machine = "";
    [ObservableProperty] private bool isAlternate;

    public ObservableCollection<RecipeInput> Inputs { get; set; } = new();

    //RelayCommand = generates a command that can be bound to a button in the UI
    [RelayCommand]
    private void AddInput() => Inputs.Add(new RecipeInput());

    [RelayCommand]
    private void RemoveInput(RecipeInput input) => Inputs.Remove(input);
}