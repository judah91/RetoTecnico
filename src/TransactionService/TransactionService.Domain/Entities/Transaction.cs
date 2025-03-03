using System.ComponentModel.DataAnnotations;

namespace TransactionService.Domain.Entities
{
    public class Transaction
    {
        [Key]
        public Guid TransactionExternalId { get; set; } = Guid.NewGuid(); 

        [Required]
        public Guid SourceAccountId { get; set; } 

        [Required]
        public Guid TargetAccountId { get; set; } 

        [Required]
        public int TransferTypeId { get; set; } 

        [Required]
        public decimal Value { get; set; } 

        public string Status { get; set; } = "Pending"; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
