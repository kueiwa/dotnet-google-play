using GooglePlay.Models;

namespace GooglePlay
{
    public class AppNotFreeException : GooglePlayApiException
    {
        public AppNotFreeException(ResponseWrapper responseWrapper)
        {
            ResponseWrapper = responseWrapper;
            Amount = responseWrapper.payload.buyResponse.checkoutinfo.item.amount.formattedAmount;
        }

        public ResponseWrapper ResponseWrapper { get; private set; }
        public string Amount { get; private set; }
    }
}