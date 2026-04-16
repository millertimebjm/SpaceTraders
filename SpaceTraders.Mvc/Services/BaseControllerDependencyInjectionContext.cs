using SpaceTraders.Services.Accounts.Interfaces;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ServerStatusServices.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Services;

public class BaseControllerDependencyInjectionContext
{
    public readonly IAgentsService AgentsService;
    public readonly IShipStatusesCacheService ShipStatusesCacheService;
    public readonly ISystemsService SystemsService;
    public readonly IAccountService AccountService;
    public readonly IServerStatusService ServerStatusService;
    public readonly IConfiguration Configuration;
    
    public BaseControllerDependencyInjectionContext(
        IAgentsService agentsService,
        IShipStatusesCacheService shipStatusesCacheService,
        ISystemsService systemsService,
        IAccountService accountService,
        IServerStatusService serverStatusService,
        IConfiguration configuration)
    {
        AgentsService = agentsService;
        ShipStatusesCacheService = shipStatusesCacheService;
        SystemsService = systemsService;
        AccountService = accountService;
        ServerStatusService = serverStatusService;
        Configuration = configuration;
    }
}
