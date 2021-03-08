

namespace SpaceTraders.Models
{
    public class Order 
    {
        public string Good { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
    }
}