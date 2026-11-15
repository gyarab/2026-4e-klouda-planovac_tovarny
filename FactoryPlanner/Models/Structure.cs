using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace FactoryPlanner.Models;


public partial class Structure : ObservableObject, INamed
{
    [ObservableProperty] private string name = "New structure";
    [ObservableProperty] private decimal powerMw;
    
    //NotifyPropertyChangedFor = updates SizeText
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SizeText))] private decimal width = 4;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SizeText))] private decimal length = 4;
    [ObservableProperty] private int tier = 1;
    [ObservableProperty] private string imageUrl = "";

    //JsonIgnore = prevents the property from being serialized to JSON
    //size text is just a human-readable representation of the size
    [JsonIgnore]
    public string SizeText => $"{Width:0.##} × {Length:0.##} m";
}