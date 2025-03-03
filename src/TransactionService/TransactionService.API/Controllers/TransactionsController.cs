using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Data;
using TransactionService.Infrastructure.Kafka;

namespace TransactionService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly KafkaProducer _kafkaProducer;

        public TransactionsController(ApplicationDbContext context, KafkaProducer kafkaProducer)
        {
            _context = context;
            _kafkaProducer = kafkaProducer;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] Transaction transaction)
        {
            transaction.TransactionExternalId = Guid.NewGuid(); 
            transaction.Status = "Pending"; 
                        
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[TransactionsController] Transacción guardada en BD con ID: {transaction.TransactionExternalId}");

            //Enviar evento a Kafka
            await _kafkaProducer.SendTransactionEventAsync(transaction);
            Console.WriteLine($"[TransactionsController] Evento enviado a Kafka para ID: {transaction.TransactionExternalId}");

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.TransactionExternalId }, transaction);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(Guid id)
        {
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionExternalId == id);
            if (transaction == null) return NotFound();
            return Ok(transaction);
        }       
    }
}