# SpaceTraders

## SpaceTraders.io Game REST API

### Suggested improvements

* Write a buy for MODULE_CARGO_HOLD_III
* Solve waypoints without marketplace getting into trade models - unsync between waypoint and system
* Fix waypoints getting removed from trade models
* fix adding trade models at new markets
* add more testing
* move all non-ship-api properties to shipstatus

### Should be solved, need verification

* Modify delete and insert to ReplaceOneAsync
* Reduce API calls for waypoint refresh
  * Marketplace refresh when not fuel and antimatter, or trade goods is empty
  * Shipyard refresh only when null
  * Construction refresh only when IsUnderConstruction or null
* Add link just to ship in ships list
* Add errors to Ship Log
* execute each ship in parallel and use a channel to execute api calls
* Reduce transports after jump gate is complete => 15 total
* Add error data to shiplog
* Add Upgrade Ship Module Ship Command
