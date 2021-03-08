using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceTraders.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Picture { get; set; }
        public string Email { get; set; }
        public long Credits { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IEnumerable<Ship> Ships { get; set; } = new List<Ship>();
        public IEnumerable<Loan> Loans { get; set; } = new List<Loan>();

        public override string ToString()
        {
            var result = $@"User {Username} ({Id}) | Credits:{Credits}"; // UpdatedAt:{UpdatedAt.ToString("s", CultureInfo.CreateSpecificCulture("en-US"))}
            if (Ships != null && Ships.Any())
            {
                result += Environment.NewLine + string.Join(Environment.NewLine, Ships.ToString());
            } 
            else 
            {
                result += Environment.NewLine + "Ships: none";
            }
            if (Loans != null && Loans.Any())
            {
                result += Environment.NewLine + string.Join(Environment.NewLine, Loans.ToString());
            }
            else 
            {
                result += Environment.NewLine + "Loans: none";
            }
            return result;
        }
    }
}