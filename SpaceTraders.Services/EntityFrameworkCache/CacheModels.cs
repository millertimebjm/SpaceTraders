namespace SpaceTraders.Services.EntityFrameworkCache;

public record AgentCacheModel(string Symbol, string AgentJson) {}
public record ShipStatusCacheModel(string Symbol, string ShipStatusJson) {}
public record SurveyCacheModel(string Signature, string WaypointSymbol, string SurveyJson) {}
public record STSystemCacheModel (string Symbol, string Waypoints, string STSystemJson) {}
public record WaypointCacheModel (string Symbol, string Type, string Traits, string SystemSymbol, string WaypointJson) {}
public record TransactionCacheModel(string Symbol) {}