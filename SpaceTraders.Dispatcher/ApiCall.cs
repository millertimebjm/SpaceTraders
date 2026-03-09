namespace SpaceTraders.Dispatcher;

public record ApiCall(
    HttpRequestMessage Request, 
    TaskCompletionSource<HttpResponseMessage> TaskCompletionSource);