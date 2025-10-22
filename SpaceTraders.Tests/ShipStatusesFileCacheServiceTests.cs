using Moq;
using SpaceTraders.Models;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.ShipStatuses;

namespace SpaceTraders.Tests;

public class ShipStatusesFileCacheServiceTests
{
    private readonly ShipStatusesFileCacheService _service;
    private const string _shipSymbol = "Ship1";
    private readonly ShipStatus _shipStatus = new ShipStatus(
        new Ship(_shipSymbol, null, null, null, null, null, null, null, null, null, null, null, null, null),
        "",
        DateTime.UtcNow
    );
    
    public ShipStatusesFileCacheServiceTests()
    {
        var fileWrapperMock = new Mock<IFileWrapper>();
        _service = new ShipStatusesFileCacheService(fileWrapperMock.Object);
        var task = _service.SetAsync(_shipStatus);
        task.Wait();
    }


    [Fact]
    public async Task Find_Single()
    {
        var shipStatusInDatabase = await _service.GetAsync(_shipSymbol);
        Assert.NotNull(shipStatusInDatabase);
        Assert.Equal(_shipSymbol, shipStatusInDatabase.Ship.Symbol);
    }

    [Fact]
    public async Task GetAsync_Null()
    {
        var shipStatusInDatabase = await _service.GetAsync("Not In Database");
        Assert.Null(shipStatusInDatabase);
    }

    [Fact]
    public async Task SetAsync_Single()
    {
        var newShipStatysSymbol = "NewShipStatysSymbol";
        var newShipStatus = new ShipStatus(
            new Ship(newShipStatysSymbol, null, null, null, null, null, null, null, null, null, null, null, null, null),
            "",
            DateTime.UtcNow
        );
        await _service.SetAsync(newShipStatus);
        var newShipStatusInDatabase = await _service.GetAsync(newShipStatysSymbol);
        Assert.NotNull(newShipStatusInDatabase);
        Assert.Equal(newShipStatus.Ship.Symbol, newShipStatusInDatabase.Ship.Symbol);
    }
}