using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS;
using Amazon.SQS.Model;
using Catalog.Application.Consumers;
using FCG.Shared.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Workers;

/// <summary>
/// Background service that polls the fcg-payment-processed SQS queue and
/// delegates each <see cref="PaymentProcessedEvent"/> to <see cref="PaymentProcessedConsumer"/>
/// </summary>
public class SqsWorker : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqsWorker> _logger;
    private readonly string _queueUrl;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SqsWorker(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SqsWorker> logger)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueUrl = configuration["SQS:PaymentProcessedQueueUrl"] ?? string.Empty;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(_queueUrl))
        {
            _logger.LogWarning("[SqsWorker] SQS:PaymentProcessedQueueUrl is not configured. Worker will not start.");
            return;
        }

        _logger.LogInformation("[SqsWorker] Starting SQS polling on {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SqsWorker] Error polling SQS queue. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("[SqsWorker] Stopped.");
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<PaymentProcessedEvent>(message.Body, _jsonOptions);
            if (evt == null)
            {
                _logger.LogWarning("[SqsWorker] Could not deserialize message {MessageId}. Skipping.", message.MessageId);
                await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var consumer = scope.ServiceProvider.GetRequiredService<PaymentProcessedConsumer>();
            await consumer.ConsumeAsync(evt, ct);

            await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SqsWorker] Failed to process message {MessageId}. Message will remain in queue.", message.MessageId);
        }
    }
}
