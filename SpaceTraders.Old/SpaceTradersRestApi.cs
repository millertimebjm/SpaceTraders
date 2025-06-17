using Newtonsoft.Json;
using RestSharp;

namespace SpaceTraders
{
    public class ErrorResponse
    {
        public Error Error { get; set; }
    }
    public class Error
    {
        public string Message { get; set; }
        public int Code { get; set; }
    }
    public class SpaceTradersRestApi : IRestApi
    {
        private readonly RestClient _client;
        public SpaceTradersRestApi(string baseUrl)
        {
            _client = new RestClient(baseUrl);
        }

        public ResultModel<T> Execute<T>(string url, RestSharp.Method method)
        {
            //var request = new RestRequest($"/users/{state.User.Username}?token={state.Token}", Method.GET);
            var request = new RestRequest(url, method);
            var response = _client.Execute(request);
            
            var model = new ResultModel<T>()
            {
                StatusCode = response.StatusCode,
                Successful = response.IsSuccessful,
            };
            if (response.IsSuccessful)
            {
                model.Model = JsonConvert.DeserializeObject<T>(response.Content);
            }
            else 
            {
                // "{\"error\":{\"message\":\"Token was invalid or missing from the request. Did you confirm sending the token as a query parameter or authorization header?\",\"code\":40101}}"
                model.ErrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content).Error.Message;
            }
            return model;
        }
    }
}
