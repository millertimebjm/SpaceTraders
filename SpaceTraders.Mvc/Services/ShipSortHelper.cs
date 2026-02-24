using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Services;

public static class ShipSortHelper
{
    public static IEnumerable<ShipStatus> ShipStatusSort(IEnumerable<ShipStatus> shipStatuses)
    {
        return shipStatuses
            .OrderBy(s => {
                // Find the last hyphen
                int hyphenIndex = s.Ship.Symbol.LastIndexOf('-');
                
                // If no hyphen exists, treat the whole thing as the "group"
                return hyphenIndex == -1 ? s.Ship.Symbol : s.Ship.Symbol[..hyphenIndex];
            })
            .ThenBy(s => {
                int hyphenIndex = s.Ship.Symbol.LastIndexOf('-');
                
                // Get the part AFTER the hyphen
                string hexPart = hyphenIndex == -1 ? s.Ship.Symbol : s.Ship.Symbol.Substring(hyphenIndex + 1);
                
                // Convert hex to int for proper numeric sorting (e.g., F < 10)
                try {
                    return Convert.ToInt32(hexPart, 16);
                } catch {
                    return 0; // Fallback for non-hex values
                }
            })
            .ToList();
    }
}