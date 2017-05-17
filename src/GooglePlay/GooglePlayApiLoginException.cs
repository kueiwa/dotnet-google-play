using System.Net.Http;

namespace GooglePlay
{
    public class GooglePlayApiLoginException : GooglePlayApiException
    {
        public GooglePlayApiLoginException(HttpResponseMessage response)
        {
            Response = response;
        }

        public GooglePlayApiLoginException(HttpResponseMessage response, string message) : base(message)
        {
            Response = response;
        }

        public HttpResponseMessage Response { get; }
    }
}