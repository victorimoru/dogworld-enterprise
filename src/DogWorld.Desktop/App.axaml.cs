using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DogWorld.Desktop.Services;
using DogWorld.Desktop.Telemetry;
using DogWorld.Desktop.ViewModels;
using DogWorld.Desktop.Views;
namespace DogWorld.Desktop;
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var endpoint = Environment.GetEnvironmentVariable("DOGWORLD_API_BASE_URL") ?? "http://localhost:5157/";
            if (!Uri.TryCreate(endpoint.TrimEnd('/') + "/", UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                throw new InvalidOperationException("DOGWORLD_API_BASE_URL must be an absolute HTTP or HTTPS URL.");
            var telemetry = DesktopTelemetry.Start(DesktopTelemetryOptions.Load(Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json")), uri);
            var http = new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(15) };
            var vm = new CatalogueViewModel(new TracedDogCatalogueClient(new DogCatalogueClient(http)), new DogDetailsViewModel(new TracedDogDetailsClient(new DogDetailsClient(http))));
            desktop.MainWindow = new MainWindow { DataContext = vm };
            desktop.Exit += (_, _) => { vm.Dispose(); http.Dispose(); telemetry?.Dispose(); };
        }
        base.OnFrameworkInitializationCompleted();
    }
}

