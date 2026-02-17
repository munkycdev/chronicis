using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        // Serve Blazor WebAssembly files
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();

        // Fallback to index.html for client-side routing
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
