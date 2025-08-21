#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services;
using SpaceTraders.Services.Marketplaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;

namespace SpaceTraders.Tests;

public class MarketplacesServicesTests
{
    [Fact]
    public void NavigationFactor()
    {
        var httpClient = new HttpClient();
        var configurationSub = Substitute.For<IConfiguration>();
        configurationSub[MarketplacesService.SPACETRADER_PREFIX + ConfigurationEnums.ApiUrl.ToString()].Returns(ConfigurationEnums.ApiUrl.ToString());
        configurationSub[MarketplacesService.SPACETRADER_PREFIX + ConfigurationEnums.AgentToken.ToString()].Returns(ConfigurationEnums.AgentToken.ToString());
        var mongoCollectionFactorySub = Substitute.For<IMongoCollectionFactory>();
        var loggerSub = Substitute.For<ILogger<MarketplacesService>>();
        var pathsServiceSub = Substitute.For<IPathsService>();
        pathsServiceSub.BuildSystemPathWithCostWithMemo("A1-AB1-ABC1", 10, 10).Returns(
            new Dictionary<Waypoint, (List<Waypoint>, int)>
            {
                [new Waypoint("A1-AB1-ABC2", "A1-AB1", "", 0, 0, null, null, null, null, null, null, false, null)] = ([], 1000),
                [new Waypoint("A1-AB1-ABC3", "A1-AB1", "", 0, 0, null, null, null, null, null, null, false, null)] = ([], 10)
            }
        );

        IMarketplacesService marketplacesService = new MarketplacesService(
            httpClient,
            configurationSub,
            mongoCollectionFactorySub,
            loggerSub,
            pathsServiceSub
        );

        var tradeSymbolBest = "TradeSymbolBest";
        var tradeSymbolWorst = "TradeSymbolWorst";

        IReadOnlyList<TradeModel> tradeModels = [
            new TradeModel(tradeSymbolWorst,"A1-AB1-ABC1", 1000, SupplyEnum.MODERATE, 1, "A1-AB1-ABC2", 2000, SupplyEnum.LIMITED, 1, .1),
            new TradeModel(tradeSymbolBest,"A1-AB1-ABC1", 1000, SupplyEnum.MODERATE, 1, "A1-AB1-ABC3", 2000, SupplyEnum.LIMITED, 1, 1)
        ];

        var tradesOrdered = marketplacesService.GetBestOrderedTradesWithTravelCost(tradeModels);
        Assert.Equal(2, tradesOrdered.Count());
        Assert.Equal(tradeSymbolBest, tradesOrdered.First().TradeSymbol);
        Assert.Equal(tradeSymbolWorst, tradesOrdered.Last().TradeSymbol);
    }
}
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.