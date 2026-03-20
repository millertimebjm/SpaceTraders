# SpaceTraders

SpaceTraders.io Game REST API

* sell all mining and siphoning ships after jump gate is complete
* Use the new pathing for all path searches
* fix adding trade models at new markets
* Fix buying contract materials to be lowest buy price within range instead of lowest name
* shipyards are generally at markets, so check if I have a probe at a shipyard instead of sending my command ship there
* execute each ship in parallel and use a channel to execute api calls
* add more testing
* move all non-ship-api properties to shipstatus
* mining ships in each system
* Add error data to shiplog

# Plan for Trade Models and Path Tracing

## Uses for Path tracing

* Building Trade models (cacheable and single marketplace updatable)
* Travel

## Caching methods for Path Tracing

* Path Tracing needs to be cached per origin
