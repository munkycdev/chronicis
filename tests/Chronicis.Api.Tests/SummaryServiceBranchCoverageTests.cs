using Chronicis.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class SummaryServiceBranchCoverageTests
{
    [Fact]
    public void SummaryService_ConstructorAndPrivateHelpers_CoverRemainingBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();

        Assert.Throws<InvalidOperationException>(() => new SummaryService(
            db,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build(),
            NullLogger<SummaryService>.Instance));

        Assert.Throws<InvalidOperationException>(() => new SummaryService(
            db,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://example.test"
            }).Build(),
            NullLogger<SummaryService>.Instance));

        Assert.Throws<InvalidOperationException>(() => new SummaryService(
            db,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://example.test",
                ["AzureOpenAI:ApiKey"] = "key"
            }).Build(),
            NullLogger<SummaryService>.Instance));

        _ = new SummaryService(
            db,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://example.test",
                ["AzureOpenAI:ApiKey"] = "key",
                ["AzureOpenAI:DeploymentName"] = "dep"
            }).Build(),
            NullLogger<SummaryService>.Instance);

        var buildPrompt = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SummaryService), "BuildPrompt");
        var promptNoWeb = (string)buildPrompt.Invoke(null, ["{EntityName}|{SourceContent}|{WebContent}", "Entity", "Source", ""])!;
        var promptWithWeb = (string)buildPrompt.Invoke(null, ["{EntityName}|{SourceContent}|{WebContent}", "Entity", "Source", "Web"])!;
        Assert.Contains("Entity|Source|", promptNoWeb);
        Assert.Contains("Additional context", promptWithWeb);

        var sourceContentType = typeof(SummaryService).Assembly.GetType("Chronicis.Api.Services.SourceContent")!;
        object MakeSource(string type, string title, string content)
        {
            var instance = Activator.CreateInstance(sourceContentType)!;
            sourceContentType.GetProperty("Type")!.SetValue(instance, type);
            sourceContentType.GetProperty("Title")!.SetValue(instance, title);
            sourceContentType.GetProperty("Content")!.SetValue(instance, content);
            return instance;
        }

        var formatArticleSources = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SummaryService), "FormatArticleSources");
        var backlinksListType = typeof(List<>).MakeGenericType(sourceContentType);
        var backlinks = Activator.CreateInstance(backlinksListType)!;
        backlinksListType.GetMethod("Add")!.Invoke(backlinks, [MakeSource("Backlink", "B1", "Body1")]);

        var withPrimary = (string)formatArticleSources.Invoke(null, [MakeSource("Primary", "P", "Canonical"), backlinks])!;
        var withoutPrimary = (string)formatArticleSources.Invoke(null, [null, Activator.CreateInstance(backlinksListType)!])!;

        Assert.Contains("CANONICAL CONTENT", withPrimary);
        Assert.Contains("REFERENCES FROM OTHER ARTICLES", withPrimary);
        Assert.Equal(string.Empty, withoutPrimary);
    }
}
