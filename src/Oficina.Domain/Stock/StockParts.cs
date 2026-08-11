using Oficina.Domain.Parts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oficina.Domain.Stock
{
    public class StockParts
    {
        public Guid Id { get; set; }
        public Guid PartId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Quantity { get; set; }
        public Part? Part { get; set; }
    }
}
