using AntiFraudService.Domain.Entities;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AntiFraudService.Infrastructure.Kafka
{
    public class KafkaConsumer : BackgroundService
    {
        private readonly string _inputTopic = "transactions-topic";
        private readonly string _outputTopic = "antifraud-results-topic";
        private readonly IConsumer<string, string> _consumer;
        private readonly IProducer<string, string> _producer;

        // Diccionario para almacenar transacciones diarias por usuario
        private readonly ConcurrentDictionary<Guid, List<TransactionDto>> _dailyTransactions;

        public KafkaConsumer()
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "antifraud-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _consumer.Subscribe(_inputTopic);

            var producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();

            _dailyTransactions = new ConcurrentDictionary<Guid, List<TransactionDto>>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    var transactionEvent = JsonSerializer.Deserialize<TransactionDto>(consumeResult.Message.Value);

                    Console.WriteLine($"[AntiFraudService] Validating transaction: {JsonSerializer.Serialize(transactionEvent)}");

                    // Validar la transacción
                    var isFraudulent = ValidateTransaction(transactionEvent, out string reason);

                    var result = new FraudCheckResult
                    {
                        TransactionExternalId = transactionEvent.TransactionExternalId, 
                        IsFraudulent = isFraudulent,
                        Reason = reason
                    };

                    // Enviar resultado a Kafka
                    var message = new Message<string, string>
                    {
                        Key = transactionEvent.TransactionExternalId.ToString(), 
                        Value = JsonSerializer.Serialize(result)
                    };

                    await _producer.ProduceAsync(_outputTopic, message);

                    Console.WriteLine($"[AntiFraudService] Sent fraud check result: {JsonSerializer.Serialize(result)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AntiFraudService] Error processing message: {ex.Message}");
                }
            }
        }

        private bool ValidateTransaction(TransactionDto transaction, out string reason)
        {
            var today = DateTime.UtcNow.Date;
            var userKey = transaction.SourceAccountId; 

            // Si la transacción supera 2000 es fraudulenta
            if (transaction.Value > 2000)
            {
                reason = "Fraud detected: Single transaction exceeds 2000";
                return true;
            }

            // Obtener la lista de transacciones diarias del usuario
            if (!_dailyTransactions.ContainsKey(userKey))
            {
                _dailyTransactions[userKey] = new List<TransactionDto>();
            }

            _dailyTransactions[userKey].Add(transaction);

            // Calcular el total de transacciones del día
            var dailyTotal = _dailyTransactions[userKey].Sum(t => t.Value);
            if (dailyTotal > 20000)
            {
                reason = "Fraud detected: Daily transaction limit exceeded ($20,000)";
                return true;
            }

            reason = "Approved";
            return false;
        }
    }
}
