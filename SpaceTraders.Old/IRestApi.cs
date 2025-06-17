using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceTraders
{
    public class ResultModel<T>
    {
        public T Model { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
        public string ErrorResponse { get; set; }
        public bool Successful { get; set; }
    }

    public interface IRestApi
    {
        ResultModel<T> Execute<T>(string url, RestSharp.Method method);
    }
}
