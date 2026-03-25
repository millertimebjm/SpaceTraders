using MongoDB.Driver;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using SpaceTraders.Services.MongoCache.Interfaces;

namespace SpaceTraders.Services.HttpHelpers;

public class ApiRequestLimiterCacheService : IApiRequestLimiterService
{
    private readonly IMongoCollection<Token> _tokens;

    public ApiRequestLimiterCacheService(IMongoCollectionFactory collectionFactory)
    {
        _tokens = collectionFactory.GetCollection<Token>();
        // Optimization: Create an Index on AgentId and Timestamp (Ascending)
        var indexKeys = Builders<Token>.IndexKeys.Ascending(t => t.Timestamp);
        _tokens.Indexes.CreateOne(new CreateIndexModel<Token>(indexKeys));
    }

    public async Task WaitUntilReadyAsync(CancellationToken ct = default)
    {
        // 1. Get the reserved time from MongoDB
        DateTime reservedTime = await RequestToken();

        // 2. Calculate how long we need to wait from "Now"
        var delay = reservedTime - DateTime.UtcNow;

        if (delay > TimeSpan.Zero)
        {
            // 3. Pause the execution until our "slot" arrives
            await Task.Delay(delay, ct);
        }
    }

    public async Task<DateTime> RequestToken()
{
    var now = DateTime.UtcNow;
    var window1s = now.AddSeconds(-1);
    var window1m = now.AddMinutes(-1);

    // 1. Get the most recent token and the counts in one "pass" (or close to it)
    // We need to know the latest scheduled time to ensure we append to the end of the "line"
    var projection = Builders<Token>.Projection.Exclude("_id");
    var lastToken = await _tokens.Find(FilterDefinition<Token>.Empty)
        .SortByDescending(t => t.Timestamp)
        .Project<Token>(projection)
        .FirstOrDefaultAsync();

    long countSec = await _tokens.CountDocumentsAsync(t => t.Timestamp > window1s);
    long countMin = await _tokens.CountDocumentsAsync(t => t.Timestamp > window1m);

    DateTime nextAvailable;

    if (countSec < 2 && countMin < 30)
    {
        // If the "last token" in the DB is in the past, we can go 'now'
        // If it's in the future (reserved by another app), we must follow it.
        nextAvailable = (lastToken == null || lastToken.Timestamp < now) 
                        ? now 
                        : lastToken.Timestamp.AddMilliseconds(501);
    }
    else
    {
        // We are throttled. We must find the specific token that is "blocking" us.
        // If it's the 1-minute limit, we wait for the 30th oldest token to expire.
        // If it's the 1-second limit, we wait for the 2nd oldest.
        int skipCount = countMin >= 30 ? 29 : 1; 

        var blockingToken = await _tokens.Find(t => t.Timestamp > window1m)
            .SortBy(t => t.Timestamp)
            .Skip(skipCount)
            .Project<Token>(projection)
            .FirstOrDefaultAsync();

        // The next slot is the blocking token's time + the required gap
        var waitBase = countMin >= 30 ? blockingToken.Timestamp.AddMinutes(1) : blockingToken.Timestamp.AddMilliseconds(501);
        
        // Ensure we don't accidentally schedule BEFORE the latest existing token
        var absoluteMin = lastToken?.Timestamp.AddMilliseconds(501) ?? now;
        nextAvailable = waitBase > absoluteMin ? waitBase : absoluteMin;
    }

    await _tokens.InsertOneAsync(new Token(nextAvailable));
    return nextAvailable;
}
}

public record Token(DateTime Timestamp);