using System;

namespace SpaceTraders.Models
{
    public class PurchaseLocation 
    {
        public string Location { get; set; }
        public decimal Price { get; set; }
        public override string ToString()
        {
            return $"Location:{Location} | Price:{Price}";
        }
    }
}