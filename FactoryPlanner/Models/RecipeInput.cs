
using CommunityToolkit.Mvvm.ComponentModel;

namespace FactoryPlanner.Models;

public partial class RecipeInput : ObservableObject
{
    [ObservableProperty] private string itemName = "";
    [ObservableProperty] private decimal amount = 1;
}