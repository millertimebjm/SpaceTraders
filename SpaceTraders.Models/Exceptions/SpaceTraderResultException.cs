using SpaceTraders.Models;

namespace SpaceTraders.Model.Exceptions;

public class SpaceTraderResultException(
    string Message,
    HttpRequestException Exception,
     string responseBody) : Exception(Message, Exception)
{
    public string ResponseBody { get; } = responseBody;
}