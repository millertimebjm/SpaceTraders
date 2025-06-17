using System;

namespace SpaceTraders.Models
{
    public class FlightPlan 
    {
        public DateTime ArrivesAt { get; set; }
        public string Destination { get; set; }
        public int FuelConsumed { get; set; }
        public int FuelRemaining { get; set; }
        public string Id { get; set; }
        public string ShipId { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public int TimeRemainingInSeconds { get; set; }
    }
}