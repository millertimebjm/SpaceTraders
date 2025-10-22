using Moq;
using SpaceTraders.Models;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Systems;

namespace SpaceTraders.Tests;

public class SystemsFileCacheServiceTests
{
    private readonly SystemsFileCacheService _service;
    private const string _systemSymbol = "A1-B2-CD3";
    private readonly STSystem _system = new STSystem(
        "", _systemSymbol, "", "", 0, 0, null, null, "" 
    );
    
    public SystemsFileCacheServiceTests()
    {
        var fileWrapperMock = new Mock<IFileWrapper>();
        _service = new SystemsFileCacheService(fileWrapperMock.Object);
        var task = _service.SetAsync(_system);
        task.Wait();
    }


    [Fact]
    public async Task Find_Single()
    {
        var systemInDatabase = await _service.GetAsync(_systemSymbol);
        Assert.NotNull(systemInDatabase);
        Assert.Equal(_systemSymbol, systemInDatabase.Symbol);
    }

    [Fact]
    public async Task GetAsync_Null()
    {
        var systemInDatabase = await _service.GetAsync("Not In Database");
        Assert.Null(systemInDatabase);
    }

    [Fact]
    public async Task SetAsync_Single()
    {
        var newSystemSymbol = "NewSystemSymbol";
        var newSystem = new STSystem(
            "", newSystemSymbol, "", "", 0, 0, null, null, "" 
        );
        await _service.SetAsync(newSystem);
        var newSystemInDatabase = await _service.GetAsync(newSystemSymbol);
        Assert.NotNull(newSystemInDatabase);
        Assert.Equal(newSystem.Symbol, newSystemInDatabase.Symbol);
    }
}