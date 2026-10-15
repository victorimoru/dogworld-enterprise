using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DogWorld.Contracts;
using DogWorld.Desktop.Services;
using DogWorld.Desktop.ViewModels;
using DogWorld.Desktop.Views;
[assembly: AvaloniaTestApplication(typeof(DogWorld.Desktop.Tests.UiApp))]
namespace DogWorld.Desktop.Tests;
public class UiApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().WithInterFont().UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
public class CatalogueUiTests
{
    [AvaloniaFact]
    public void CatalogueRendersAndPaginationCommandsChangePage()
    {
        using var vm = new CatalogueViewModel(new DemoClient());
        var window = new MainWindow { DataContext = vm };
        try
        {
            window.Show();
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Bella");
            Assert.False(vm.PreviousCommand.CanExecute(null));
            Assert.True(vm.NextCommand.CanExecute(null));
            var output = Environment.GetEnvironmentVariable("DOGWORLD_DESKTOP_PREVIEW");
            if (!string.IsNullOrWhiteSpace(output)) frame.Save(output, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            vm.NextCommand.Execute(null);
            Assert.Equal(2, vm.Page);
            Assert.False(vm.NextCommand.CanExecute(null));
            Assert.True(vm.PreviousCommand.CanExecute(null));
        }
        finally { window.Close(); }
    }
    private sealed class DemoClient : IDogCatalogueClient
    {
        private static readonly string[] Names = ["Bella", "Max", "Charlie", "Luna", "Cooper", "Daisy", "Milo", "Zoe", "Rocky", "Lola", "Bear", "Bailey"];
        private static readonly string[] Breeds = ["Golden Retriever", "German Shepherd", "French Bulldog", "Labrador Retriever", "Beagle", "Poodle"];
        public Task<DogPage> GetPageAsync(int page,int pageSize,CancellationToken cancellationToken) =>
            Task.FromResult(new DogPage(Enumerable.Range(0,page == 1 ? 12 : 1)
                .Select(i => new AvailableDog(i+1,Names[i],Breeds[i%6],14+i)).ToArray(),13));
    }
}
