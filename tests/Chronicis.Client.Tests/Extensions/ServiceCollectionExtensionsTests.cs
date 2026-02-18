using Chronicis.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddChronicisApiService_RegistersInterfaceImplementation()
    {
        var services = CreateServices();

        var returned = services.AddChronicisApiService<ITestApiService, TestApiService>("TestClient");

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestApiService>();
        Assert.IsType<TestApiService>(service);
        Assert.Equal("https://api.test/", service.HttpClient.BaseAddress!.ToString());
    }

    [Fact]
    public void AddChronicisApiServiceWithSnackbar_RegistersService()
    {
        var services = CreateServices();
        services.AddSingleton(Substitute.For<ISnackbar>());

        var returned = services.AddChronicisApiServiceWithSnackbar<ITestSnackbarApiService, TestSnackbarApiService>("TestClient");

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestSnackbarApiService>();
        Assert.IsType<TestSnackbarApiService>(service);
        Assert.NotNull(service.Snackbar);
    }

    [Fact]
    public void AddChronicisApiServiceWithJSRuntime_RegistersService()
    {
        var services = CreateServices();
        services.AddSingleton(Substitute.For<IJSRuntime>());

        var returned = services.AddChronicisApiServiceWithJSRuntime<ITestJsApiService, TestJsApiService>("TestClient");

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestJsApiService>();
        Assert.IsType<TestJsApiService>(service);
        Assert.NotNull(service.JsRuntime);
    }

    [Fact]
    public void AddChronicisApiServiceConcrete_RegistersConcreteType()
    {
        var services = CreateServices();

        var returned = services.AddChronicisApiServiceConcrete<TestConcreteApiService>("TestClient");

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<TestConcreteApiService>();
        Assert.Equal("https://api.test/", service.HttpClient.BaseAddress!.ToString());
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("TestClient", client => client.BaseAddress = new Uri("https://api.test/"));
        return services;
    }

    public interface ITestApiService
    {
        HttpClient HttpClient { get; }
    }

    private sealed class TestApiService : ITestApiService
    {
        public TestApiService(HttpClient httpClient, ILogger<TestApiService> _)
        {
            HttpClient = httpClient;
        }

        public HttpClient HttpClient { get; }
    }

    public interface ITestSnackbarApiService
    {
        ISnackbar Snackbar { get; }
    }

    private sealed class TestSnackbarApiService : ITestSnackbarApiService
    {
        public TestSnackbarApiService(HttpClient _, ILogger<TestSnackbarApiService> __, ISnackbar snackbar)
        {
            Snackbar = snackbar;
        }

        public ISnackbar Snackbar { get; }
    }

    public interface ITestJsApiService
    {
        IJSRuntime JsRuntime { get; }
    }

    private sealed class TestJsApiService : ITestJsApiService
    {
        public TestJsApiService(HttpClient _, IJSRuntime jsRuntime, ILogger<TestJsApiService> __)
        {
            JsRuntime = jsRuntime;
        }

        public IJSRuntime JsRuntime { get; }
    }

    private sealed class TestConcreteApiService
    {
        public TestConcreteApiService(HttpClient httpClient, ILogger<TestConcreteApiService> _)
        {
            HttpClient = httpClient;
        }

        public HttpClient HttpClient { get; }
    }
}

