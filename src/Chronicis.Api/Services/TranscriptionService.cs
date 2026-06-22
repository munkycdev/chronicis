using Azure;
using Azure.AI.OpenAI;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using OpenAI.Chat;

namespace Chronicis.Api.Services;

/// <summary>
/// Transcription service that uses Azure OpenAI GPT-4 Vision to convert handwritten note images to text.
/// </summary>
public sealed class TranscriptionService : ITranscriptionService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<TranscriptionService> _logger;
    private readonly TimeSpan _timeout;
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    public TranscriptionService(IConfiguration configuration, ILogger<TranscriptionService> logger)
        : this(configuration, logger, DefaultTimeout)
    {
    }

    internal TranscriptionService(IConfiguration configuration, ILogger<TranscriptionService> logger, TimeSpan timeout)
    {
        _logger = logger;
        _timeout = timeout;

        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var deploymentName = configuration["AzureOpenAI:VisionDeploymentName"]
            ?? configuration["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException("AzureOpenAI configuration is incomplete for transcription.");
        }

        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = client.GetChatClient(deploymentName);
    }

    // Test constructor
    internal TranscriptionService(ChatClient chatClient, ILogger<TranscriptionService> logger, TimeSpan timeout)
    {
        _chatClient = chatClient;
        _logger = logger;
        _timeout = timeout;
    }

    public async Task<TranscriptionResultDto> TranscribeImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

        try
        {
            var imageData = BinaryData.FromBytes(imageBytes);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a transcription assistant. Transcribe the handwritten text in the image exactly as written. Output only the transcribed text, preserving paragraph breaks. Do not add commentary or formatting."),
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart("Transcribe the handwritten text in this image:"),
                    ChatMessageContentPart.CreateImagePart(imageData, "image/png"))
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 4000
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, linkedCts.Token);
            var text = completion.Value.Content[0].Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                return new TranscriptionResultDto
                {
                    Success = false,
                    ErrorMessage = "Transcription produced no text."
                };
            }

            return new TranscriptionResultDto
            {
                Success = true,
                Text = text
            };
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogErrorSanitized("Transcription request timed out after {Timeout} seconds", _timeout.TotalSeconds);
            return new TranscriptionResultDto
            {
                Success = false,
                ErrorMessage = $"Transcription timed out after {(int)_timeout.TotalSeconds} seconds."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Transcription failed");
            return new TranscriptionResultDto
            {
                Success = false,
                ErrorMessage = "Transcription service failed."
            };
        }
    }
}
