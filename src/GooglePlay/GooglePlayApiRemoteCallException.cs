using System;

namespace GooglePlay
{
    public class GooglePlayApiRemoteCallException : GooglePlayApiException
    {
        public GooglePlayApiRemoteCallException()
        {
        }

        public GooglePlayApiRemoteCallException(string message) : base(message)
        {
        }

        public GooglePlayApiRemoteCallException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}