using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FactoryPlanner.Models;

public partial class Item : ObservableObject, INamed
{
    //Observable property = notifies the UI when its value changes
    [ObservableProperty] private string name = "New item";
    [ObservableProperty] private string category = "";
    [ObservableProperty] private int stackSize = 100;
    [ObservableProperty] private string imageUrl = "";
}