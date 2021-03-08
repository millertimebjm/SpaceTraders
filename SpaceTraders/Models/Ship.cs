using System;
using System.Collections.Generic;

namespace SpaceTraders.Models
{
    public class Ship
    {
        // General Info
        public string Class { get; set; }
        public string Manufacturer { get; set; }
        public decimal MaxCargo { get; set; }
        public int Plating { get; set; }
        public IEnumerable<PurchaseLocation> PurchaseLocations { get; set; }
        public int Speed { get; set; }
        public string Type { get; set; }
        public int Weapons { get; set; }

        // Owned
        public IEnumerable<Cargo> Cargo { get; set; }
        public string Id { get; set; }
        public string Location { get; set; }
        public decimal SpaceAvailable { get; set; }
    }
}