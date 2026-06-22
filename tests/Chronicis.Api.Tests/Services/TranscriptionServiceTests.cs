using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenAI.Chat;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class TranscriptionServiceTests
{
    private static readonly byte[] SampleImageBytes = [0x89, 0x50, 0x4E, 0x47];

    private static IConfiguration CreateConfig(
        string endpoint = "https://openai.test/",
        string apiKey = "test-key",
        string deploymentName = "gpt-4o")
    {
        var configData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = endpoint,
            ["AzureOpenAI:ApiKey"] = apiKey,
            ["AzureOpenAI:DeploymentName"] = deploymentName
        };
        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }

    [Fact]
    public void Constructor_ThrowsWhenEndpointMissing()
    {
        var config = CreateConfig(endpoint: "");
        Assert.Throws<InvalidOperationException>(() =>
            new TranscriptionService(config, NullLogger<TranscriptionService>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsWhenApiKeyMissing()
    {
        var config = CreateConfig(apiKey: "");
        Assert.Throws<InvalidOperationException>(() =>
            new TranscriptionService(config, NullLogger<TranscriptionService>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsWhenDeploymentNameMissing()
    {
        var config = CreateConfig(deploymentName: "");
        Assert.Throws<InvalidOperationException>(() =>
            new TranscriptionService(config, NullLogger<TranscriptionService>.Instance));
    }

    [Fact]
    public void Constructor_UsesVisionDeploymentNameWhenPresent()
    {
        var configData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://openai.test/",
            ["AzureOpenAI:ApiKey"] = "test-key",
            ["AzureOpenAI:DeploymentName"] = "gpt-4-mini",
            ["AzureOpenAI:VisionDeploymentName"] = "gpt-4o"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

        // Should not throw — VisionDeploymentName takes priority
        var ex = Record.Exception(() =>
            new TranscriptionService(config, NullLogger<TranscriptionService>.Instance));
        Assert.Null(ex);
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenChatClientThrows()
    {
        var chatClient = Substitute.For<ChatClient>();
        chatClient.CompleteChatAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var sut = new TranscriptionService(chatClient, NullLogger<TranscriptionService>.Instance, TimeSpan.FromSeconds(60));

        var result = await sut.TranscribeImageAsync(SampleImageBytes);

        Assert.False(result.Success);
        Assert.Equal("Transcription service failed.", result.ErrorMessage);
    }

    [Fact]
    public async Task TranscribeImageAsync_ThrowsOperationCanceled_WhenCallerCancels()
    {
        var chatClient = Substitute.For<ChatClient>();
        chatClient.CompleteChatAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var sut = new TranscriptionService(chatClient, NullLogger<TranscriptionService>.Instance, TimeSpan.FromSeconds(60));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.TranscribeImageAsync(SampleImageBytes, cts.Token));
    }
}
