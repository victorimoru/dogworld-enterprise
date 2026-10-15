using Avalonia.Controls;
using DogWorld.Desktop.ViewModels;
namespace DogWorld.Desktop.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            if (DataContext is CatalogueViewModel vm) await vm.LoadAsync();
        };
        Closed += (_, _) => (DataContext as CatalogueViewModel)?.Dispose();
    }
}
