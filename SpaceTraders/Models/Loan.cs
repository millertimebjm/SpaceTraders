using System;
using System.Globalization;

namespace SpaceTraders.Models
{
    public class Loan
    {
        // General Info
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public bool CollateralRequired { get; set; }
        public decimal Rate { get; set; }
        public int TermInDays { get; set; }

        public bool HasLoan { 
            get 
            {
                return !string.IsNullOrWhiteSpace(Id);
            }
        }

        // Owned Loan
        public DateTime Due { get; set; }
        public string Id { get; set; }
        public decimal RepaymentAmount { get; set; }
        public LoanStatusEnum Status { get; set; }

        public override string ToString() 
        {
            var collateralRequiredString = CollateralRequired ? "Y" : "N";
            if (HasLoan) 
            {
                return $"Loan {Status} Due:{Due.ToString("s", CultureInfo.CreateSpecificCulture("en-US"))} | {Type} {RepaymentAmount}({Amount})  Collateral:{collateralRequiredString} {Rate} Term:{TermInDays}";
            } 
            else 
            {
                return $"Loan {Type} | {Amount} Collateral:{collateralRequiredString} {Rate} Term:{TermInDays}";
            }
            
        }
    }
}