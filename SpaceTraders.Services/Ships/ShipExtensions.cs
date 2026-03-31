using SpaceTraders.Models;

namespace SpaceTraders.Services.Ships;

public static class ShipExtensions
{
    public static IEnumerable<Ship> HexadecimalSort(this IEnumerable<Ship> ships)
    {
        return ships.OrderBy(s => {
            var parts = s.Symbol.Split('-');
            return Convert.ToInt32(parts[1], 16); // Parse as hex
        });
    }

    public static IEnumerable<ShipStatus> HexadecimalSort(this IEnumerable<ShipStatus> ships)
    {
        return ships.OrderBy(s => {
            var parts = s.Ship.Symbol.Split('-');
            return Convert.ToInt32(parts[1], 16); // Parse as hex
        });
    }
}