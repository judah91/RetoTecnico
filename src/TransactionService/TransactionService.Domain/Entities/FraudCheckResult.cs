using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionService.Domain.Entities
{
    public class FraudCheckResult
    {
        [Key]
        public Guid TransactionExternalId { get; set; }

        public bool IsFraudulent { get; set; } // Indica si la transacción es fraudulenta

        public string Reason { get; set; } // Motivo del rechazo (si aplica)
    }
}
