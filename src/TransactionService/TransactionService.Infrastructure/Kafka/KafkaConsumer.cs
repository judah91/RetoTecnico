using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Data;
using static Confluent.Kafka.ConfigPropertyNames;

namespace TransactionService.Infrastructure.Kafka
{
    public class KafkaConsumer : BackgroundService
    {
        private readonly string _topic = "antifraud-results-topic";
        private readonly IConsumer<string, string> _consumer;
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public KafkaConsumer(ILogger<KafkaConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = "localhost:9092",
                    GroupId = "transaction-service-group",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                _consumer = new ConsumerBuilder<string, string>(config).Build();
                _consumer.Subscribe(_topic);

                var producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };
                _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing KafkaConsumer");
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(5));

                    if (consumeResult == null)
                    {
                        _logger.LogInformation("[KafkaConsumer] No messages received, waiting...");
                        await Task.Delay(2000, stoppingToken);
                        continue;
                    }

                    var fraudCheckResult = JsonSerializer.Deserialize<FraudCheckResult>(
                        consumeResult.Message.Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    _logger.LogInformation($"[KafkaConsumer] Fraud check result received: {JsonSerializer.Serialize(fraudCheckResult)}");

                    if (fraudCheckResult != null)
                    {
                        await ProcessFraudCheckResult(fraudCheckResult);
                    }                    
                }               
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[KafkaConsumer] Unexpected error.");
                }
            }            
        }

        //Update State in DB
        private async Task ProcessFraudCheckResult(FraudCheckResult result)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope()) //Crear un nuevo scope para obtener el DbContext
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.TransactionExternalId == result.TransactionExternalId);

                    if (transaction == null)
                    {
                        _logger.LogWarning($"[KafkaConsumer] No transaction found with ID {result.TransactionExternalId}");
                        // Enviar mensaje a la "Dead Letter Queue" para recuperación ante fallos
                        await SendToDeadLetterQueue(result);
                        return;
                    }

                    transaction.Status = result.IsFraudulent ? "Rejected" : "Approved";
                    await context.SaveChangesAsync();

                    _logger.LogInformation($"[KafkaConsumer] Updated transaction {transaction.TransactionExternalId} status to {transaction.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KafkaConsumer] Error updating transaction status.");
            }
        }

        private async Task SendToDeadLetterQueue(FraudCheckResult result)
        {
            var dlqMessage = new Message<string, string>
            {
                Key = result.TransactionExternalId.ToString(),
                Value = JsonSerializer.Serialize(result)
            };

            await _producer.ProduceAsync("dead-letter-topic", dlqMessage);
            _logger.LogError($"[KafkaConsumer] Message sent to Dead Letter Queue: {JsonSerializer.Serialize(result)}");
        }


    }
}
