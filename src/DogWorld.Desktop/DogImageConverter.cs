using Avalonia.Controls;
using Avalonia;
using Avalonia.Data.Converters;
using System.Globalization;
namespace DogWorld.Desktop;
public sealed class DogImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Application.Current!.FindResource($"Dog{((value is int id ? id : 0) % 3 + 3) % 3 + 1}");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
