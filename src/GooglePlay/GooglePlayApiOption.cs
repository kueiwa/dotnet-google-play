namespace GooglePlay
{
    public class GooglePlayApiOption
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string AndroidId { get; set; }
        public string AuthToken { get; set; }
        public string Language { get; set; } = "en_US";

        public bool Debug { get; set; } = false;
        public string SdkVersion { get; set; } = "23";
        public string CountryCode { get; set; } = "us";

        public string ApiUserAgent { get; set; } =
            "Android-Finsky/5.2.13 (api=3,versionCode=80321300,sdk=22,device=mako,hardware=mako,product=occam,platformVersionRelease=5.1.1,model=Nexus%204,buildId=LMY48T,isWideScreen=0)"
            ;

        public string DownloadUserAgent { get; set; } =
            "AndroidDownloadManager/5.1.1 (Linux; U; Android 5.1.1; Nexus 4 Build/LMY48T)";

        public bool PreFetch { get; set; } = false;
        public int CacheInvalidationInterval { get; set; } = 3000;
        public string DeviceCountry { get; set; } = "us";
        public string ClientId { get; set; } = "am-android-google";
        public string AndroidVending { get; set; } = "com.android.vending";
        public string AccountType { get; set; } = "HOSTED_OR_GOOGLE";
        public string Service { get; set; } = "androidmarket";
        public string LoginUrl { get; set; } = "https://android.clients.google.com/auth";

        public string[] UnsupportedExperiments { get; set; } = {
            "nocache:billing.use_charging_poller",
            "market_emails", "buyer_currency", "prod_baseline",
            "checkin.set_asset_paid_app_field", "shekel_test", "content_ratings",
            "buyer_currency_in_app", "nocache:encrypted_apk", "recent_changes"
        };

        public string[] EnabledExperiments { get; set; } = {"cl:billing.select_add_instrument_by_default"};
    }
}