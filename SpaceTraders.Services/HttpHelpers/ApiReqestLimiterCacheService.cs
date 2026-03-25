// using MongoDB.Driver;
// using SpaceTraders.Services.HttpHelpers.Interfaces;
// using SpaceTraders.Services.MongoCache.Interfaces;

// namespace SpaceTraders.Services.HttpHelpers;

// public class ApiRequestLimiterCacheService : IApiRequestLimiterService
// {
//     private readonly IMongoCollection<Token> _tokens;

//     public ApiRequestLimiterCacheService(IMongoCollectionFactory collectionFactory)
//     {
//         _tokens = collectionFactory.GetCollection<Token>();
//         // Optimization: Create an Index on AgentId and Timestamp (Ascending)
//         var indexKeys = Builders<Token>.IndexKeys.Ascending(t => t.Timestamp);
//         _tokens.Indexes.CreateOne(new CreateIndexModel<Token>(indexKeys));
//     }

//     public async Task WaitUntilReadyAsync(CancellationToken ct = default)
//     {
//         // 1. Get the reserved time from MongoDB
//         DateTime reservedTime = await RequestToken();

//         // 2. Calculate how long we need to wait from "Now"
//         var delay = reservedTime - DateTime.UtcNow;

//         if (delay > TimeSpan.Zero)
//         {
//             // 3. Pause the execution until our "slot" arrives
//             await Task.Delay(delay, ct);
//         }
//     }

//     public async Task<DateTime> RequestToken()
//     {
//         var now = DateTime.UtcNow;
//         var window1s = now.AddSeconds(-1);
//         var window1m = now.AddMinutes(-1);

//         // CRITICAL: Delete tokens older than 1 minute so they don't bloat your counts
//         await _tokens.DeleteManyAsync(t => t.Timestamp < window1m);

//         var projection = Builders<Token>.Projection.Exclude("_id");
//         // Get the most recent scheduled token
//         var lastToken = await _tokens.Find(FilterDefinition<Token>.Empty)
//             .SortByDescending(t => t.Timestamp)
//             .Project<Token>(projection)
//             .FirstOrDefaultAsync();

//         long countSec = await _tokens.CountDocumentsAsync(t => t.Timestamp > window1s);
//         long countMin = await _tokens.CountDocumentsAsync(t => t.Timestamp > window1m);

//         DateTime nextAvailable;

//         // If we haven't hit any limits...
//         if (countSec < 2 && countMin < 30)
//         {
//             // If the last scheduled token is way in the past, start at 'now'
//             // Otherwise, stack it 501ms after the last one to respect the 2/s limit
//             nextAvailable = (lastToken == null || lastToken.Timestamp < now) 
//                             ? now 
//                             : lastToken.Timestamp.AddMilliseconds(501);
//         }
//         else
//         {
//             // Identify which limit we hit
//             bool isMinuteLimit = countMin >= 30;
            
//             // Find the oldest token in the window that needs to "expire"
//             var blockingToken = await _tokens.Find(t => t.Timestamp > (isMinuteLimit ? window1m : window1s))
//                 .SortBy(t => t.Timestamp)
//                 .Project<Token>(projection)
//                 .FirstOrDefaultAsync();

//             // Calculate when that slot opens up
//             var waitBase = isMinuteLimit 
//                 ? blockingToken.Timestamp.AddMinutes(1) 
//                 : blockingToken.Timestamp.AddSeconds(1); 
                
//             // Ensure we never schedule a token earlier than 501ms after the previous one
//             var absoluteMin = lastToken.Timestamp.AddMilliseconds(501);
//             nextAvailable = waitBase > absoluteMin ? waitBase : absoluteMin;
//         }

//         await _tokens.InsertOneAsync(new Token(nextAvailable));
//         return nextAvailable;
//     }
// }

// public record Token(DateTime Timestamp);