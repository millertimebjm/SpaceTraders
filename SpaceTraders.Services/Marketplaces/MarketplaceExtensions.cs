using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services.Marketplaces;

public static class MarketplaceExtensions
{
    public static bool HasTradeGood(this Marketplace? marketplace, TradeSymbolsEnum symbol)
    {
        if (marketplace?.Exchange.Any(e => e.Symbol == symbol.ToString()) == true
            || marketplace?.Imports.Any(e => e.Symbol == symbol.ToString()) == true
            || marketplace?.Exports.Any(e => e.Symbol == symbol.ToString()) == true)
            return true;
        return false;
    }
}