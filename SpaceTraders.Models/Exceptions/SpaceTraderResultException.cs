using SpaceTraders.Models;

namespace SpaceTraders.Model.Exceptions;

// This is the Primary Constructor
public class SpaceTraderResultException(
    string Message,
    HttpRequestException? Exception,
    string responseBody) : Exception(Message, Exception)
{
    public SpaceTraderResultException(string? message) 
        : this(message ?? string.Empty, null, string.Empty)
    {
    }

    public string ResponseBody { get; } = responseBody;
}