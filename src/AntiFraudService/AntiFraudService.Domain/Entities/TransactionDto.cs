using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiFraudService.Domain.Entities
{
    public class TransactionDto
    {
        public Guid TransactionExternalId { get; set; } // ✅ Cambiado a GUID
        public Guid SourceAccountId { get; set; } // ✅ Renombrado
        public Guid TargetAccountId { get; set; } // ✅ Renombrado
        public decimal Value { get; set; } // ✅ Cambiado de Amount a Value
    }
}
