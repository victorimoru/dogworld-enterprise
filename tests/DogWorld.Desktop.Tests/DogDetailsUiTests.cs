using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DogWorld.Contracts;
using DogWorld.Desktop.Services;
using DogWorld.Desktop.ViewModels;
using DogWorld.Desktop.Views;
namespace DogWorld.Desktop.Tests;
public class DogDetailsUiTests
{
    [AvaloniaFact]
    public async Task TileOpensDetailsAndClosingPreservesCataloguePage()
    {
        using var details = new DogDetailsViewModel(new DetailsClient());
        using var vm = new CatalogueViewModel(new CatalogueClient(), details);
        var window = new MainWindow { DataContext = vm };
        try
        {
            window.Show();
            await vm.LoadAsync(2,TestContext.Current.CancellationToken);
            using(var firstFrame = window.CaptureRenderedFrame()) Assert.NotNull(firstFrame);
            var tile = window.GetVisualDescendants().OfType<Button>().Single(button => button.CommandParameter is int id && id == 1);
            Assert.Same(vm.SelectDogCommand,tile.Command);
            tile.Command!.Execute(tile.CommandParameter);
            using var frame = window.CaptureRenderedFrame();
            Assert.True(details.IsOpen);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Rabies" && text.IsEffectivelyVisible);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Friendly and affectionate" && text.IsEffectivelyVisible);
            var output = Environment.GetEnvironmentVariable("DOGWORLD_DETAILS_PREVIEW");
            if(!string.IsNullOrWhiteSpace(output)) frame!.Save(output,Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            details.CloseCommand.Execute(null);
            using var closedFrame = window.CaptureRenderedFrame();
            Assert.False(window.GetVisualDescendants().OfType<DogDetailsPanel>().Single().IsVisible);
            Assert.Equal(2,vm.Page);
            Assert.Single(vm.Dogs);
        }
        finally { window.Close(); }
    }
    private sealed class CatalogueClient : IDogCatalogueClient
    {
        public Task<DogPage> GetPageAsync(int page,int pageSize,CancellationToken token) => Task.FromResult(new DogPage([new(1,"Bella","Golden Retriever",14)],13));
    }
    private sealed class DetailsClient : IDogDetailsClient
    {
        public Task<DogDetails?> GetAsync(int id,CancellationToken token) => Task.FromResult<DogDetails?>(new(id,"Bella","Golden Retriever",14,"Available")
        {
            BreedDetails = new("golden-retriever","Golden Retriever","Sporting","Friendly and affectionate","10–12 years","25–34 kg","Scotland"),
            Vaccinations = [new(1,id,"Rabies",new DateOnly(2026,4,12),new DateOnly(2027,4,12)),new(2,id,"DHPP",new DateOnly(2026,5,20),null)]
        });
    }
}
