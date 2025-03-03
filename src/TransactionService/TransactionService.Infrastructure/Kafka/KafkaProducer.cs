using Confluent.Kafka;
using System.Text.Json;

namespace TransactionService.Infrastructure.Kafka
{
    public class KafkaProducer
    {
        private readonly string _topic = "transactions-topic";
        private readonly IProducer<string, string> _producer;

        public KafkaProducer()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092" // Puerto por el que corre Kafka en Docker
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task SendTransactionEventAsync(object transactionEvent)
        {
            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = JsonSerializer.Serialize(transactionEvent)
            };

            await _producer.ProduceAsync(_topic, message);
        }
    }
}
