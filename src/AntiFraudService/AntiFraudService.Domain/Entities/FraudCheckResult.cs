using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiFraudService.Domain.Entities
{
    public class FraudCheckResult
    {
        public Guid TransactionExternalId { get; set; } // ✅ Cambiado a GUID
        public bool IsFraudulent { get; set; }
        public string Reason { get; set; }
    }
}
