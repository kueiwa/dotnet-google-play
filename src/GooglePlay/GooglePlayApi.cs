using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GooglePlay.Models;
using ProtoBuf;

namespace GooglePlay
{
    public class GooglePlayApi : HttpClient
    {
        private readonly GooglePlayApiOption _option;

        public GooglePlayApi(GooglePlayApiOption googlePlayApiOption)
            : this(googlePlayApiOption, new HttpClientHandler())
        {
        }

        public GooglePlayApi(GooglePlayApiOption googlePlayApiOption, HttpMessageHandler handler) : base(handler)
        {
            if (googlePlayApiOption == null)
            {
                throw new ArgumentNullException(nameof(googlePlayApiOption));
            }
            _option = googlePlayApiOption;
        }

        public string AuthToken => _option.AuthToken;

        #region Main

        public async Task<ResponseWrapper> ExecuteRequestApi(string path, IDictionary<string, string> query,
            HttpContent datapost)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path), "need path");
            }
            await Login();

            using (var request = new HttpRequestMessage())
            {
                var sbUrl = new StringBuilder("https://android.clients.google.com/fdfe/");
                sbUrl.Append(path);
                if (query != null && query.Any())
                {
                    sbUrl.Append('?');
                    foreach (var item in query)
                    {
                        sbUrl.Append(Uri.EscapeUriString(item.Key));
                        sbUrl.Append('=');
                        sbUrl.Append(Uri.EscapeUriString(item.Value));
                        sbUrl.Append('&');
                    }
                    sbUrl.Length--;
                }
                request.RequestUri = new Uri(sbUrl.ToString());

                if (datapost != null)
                {
                    request.Method = HttpMethod.Post;
                    request.Content = datapost;
                }
                else
                {
                    request.Method = HttpMethod.Get;
                }

                request.Headers.AcceptLanguage.ParseAdd(_option.Language);
                request.Headers.Authorization = AuthenticationHeaderValue.Parse($"GoogleLogin auth={_option.AuthToken}");
                request.Headers.Add("X-DFE-Enabled-Experiments", string.Join(",", _option.EnabledExperiments));
                request.Headers.Add("X-DFE-Unsupported-Experiments", string.Join(",", _option.UnsupportedExperiments));
                request.Headers.Add("X-DFE-Device-Id", _option.AndroidId);
                request.Headers.Add("X-DFE-Client-Id", _option.ClientId);
                request.Headers.UserAgent.ParseAdd(_option.ApiUserAgent);
                request.Headers.Add("X-DFE-SmallestScreenWidthDp", "320");
                request.Headers.Add("X-DFE-Filter-Level", "3");
                request.Headers.Host = "android.clients.google.com";
                if (!_option.PreFetch)
                {
                    request.Headers.Add("X-DFE-No-Prefetch", "true");
                }
                using (var response = await SendAsync(request))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        string msg;
                        var contentType = response.Content.Headers.ContentType.MediaType;
                        if (contentType == "application/x-gzip" || contentType == "application/protobuf")
                        {
                            try
                            {
                                using (var ms = new MemoryStream(await response.Content.ReadAsByteArrayAsync()))
                                {
                                    var responseWrapper = Serializer.Deserialize<ResponseWrapper>(ms);
                                    msg = responseWrapper.commands.displayErrorMessage;
                                }
                            }
                            catch (Exception e)
                            {
                                throw new GooglePlayApiRemoteCallException("please report this error", e);
                            }
                        }
                        else
                        {
                            msg = await response.Content.ReadAsStringAsync();
                        }
                        throw new GooglePlayApiRemoteCallException(msg);
                    }

                    using (var ms = await response.Content.ReadAsStreamAsync())
                    {
                        return Serializer.Deserialize<ResponseWrapper>(ms);
                    }
                }
            }
        }

        #endregion

        public async Task<List<DocV2>> Search(string term, int take, int skip)
        {
            if (take > 100)
            {
                take = 100;
            }
            if (take <= 0)
            {
                take = 20;
            }
            if (skip < 0)
            {
                skip = 0;
            }
            var query = new Dictionary<string, string>
            {
                ["q"] = term,
                ["c"] = "3",
                ["n"] = take.ToString(),
                ["o"] = skip.ToString()
            };
            var content = await ExecuteRequestApi("search", query, null);
            return content.payload.searchResponse.doc;
        }

        public async Task<DocV2> GetDetails(string pkg)
        {
            var response = await ExecuteRequestApi("details", new Dictionary<string, string> {["doc"] = pkg}, null);
            return response.payload.detailsResponse.docV2;
        }

        public async Task<IEnumerable<BulkDetailsEntry>> BulkDetails(IEnumerable<string> packages)
        {
            var input = new BulkDetailsRequest
            {
                includeChildDocs = true,
                includeDetails = true
            };
            input.docid.AddRange(packages);
            HttpContent httpContent; //
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, input);
                httpContent = new ByteArrayContent(ms.ToArray());
            }
            httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-protobuf");
            var response = await ExecuteRequestApi("bulkDetails", null, httpContent);
            return response.payload.bulkDetailsResponse.entry;
        }

        public async Task<BrowseResponse> Browse(string categoryId, string subCategoryId)
        {
            var query = new Dictionary<string, string>
            {
                ["c"] = "3"
                //["cat"] = categoryId,
                //["ctr"] = subCategoryId
            };
            if (!string.IsNullOrWhiteSpace(categoryId))
            {
                query["cat"] = categoryId;
            }
            if (!string.IsNullOrWhiteSpace(subCategoryId))
            {
                query["ctr"] = subCategoryId;
            }
            var response = await ExecuteRequestApi("browse", query, null);
            return response.payload.browseResponse;
        }

        public async Task<List<BrowseLink>> GetCategories()
        {
            var r = await Browse(string.Empty, string.Empty);
            return r.category;
        }

        public async Task<AndroidAppDeliveryData> GetDownloadInfo(string package, int versionCode)
        {
            var postData = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["ot"] = "1",
                    ["doc"] = package,
                    ["vc"] = versionCode.ToString()
                }
            );
            var response = await ExecuteRequestApi("purchase", null, postData);
            if (response.payload.buyResponse.purchaseStatusResponse == null)
            {
                throw new AppNotFreeException(response);
            }
            return response.payload.buyResponse.purchaseStatusResponse.appDeliveryData;
        }

        public async Task<List<DocV2>> GetRelatedApps(string pkg)
        {
            var response = await ExecuteRequestApi("rec", new Dictionary<string, string>
            {
                ["doc"] = pkg,
                ["rt"] = "1",
                ["c"] = "3"
            }, null);
            return response.payload.listResponse.doc;
        }

        public async Task<GetReviewsResponse> GetReviews(string pkg, int nbResults, int offset)
        {
            if (nbResults > 20)
            {
                nbResults = 20;
            }
            if (offset < 0)
            {
                offset = 0;
            }
            var query = new Dictionary<string, string>
            {
                ["doc"] = pkg,
                ["c"] = "3",
                ["n"] = nbResults.ToString(),
                ["o"] = offset.ToString()
            };
            var response = await ExecuteRequestApi("rev", query, null);
            return response.payload.reviewResponse.getResponse;
        }

        #region  Login

        protected virtual Dictionary<string, string> ResponseToObject(string str)
        {
            var lines = str.Split('\n');
            var dic = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var items = line.Split('=');
                if (items.Length == 2)
                {
                    dic[items[0]] = items[1];
                }
                //assert(pair.length === 2, 'expected list of pairs from server');
            }
            return dic;
        }

        public async Task<string> Login(bool force = false)
        {
            if (string.IsNullOrWhiteSpace(_option.Username) || string.IsNullOrWhiteSpace(_option.Password))
            {
                if (string.IsNullOrWhiteSpace(_option.AuthToken))
                {
                    throw new GooglePlayApiException("You must provide a username and password or set the auth token.");
                }
            }
            if (string.IsNullOrWhiteSpace(_option.AuthToken) || force)
            {
                var body = new Dictionary<string, string>
                {
                    ["Email"] = _option.Username,
                    ["Passwd"] = _option.Password,
                    ["service"] = _option.Service,
                    ["accountType"] = _option.AccountType,
                    ["has_permission"] = "1",
                    ["source"] = "android",
                    ["androidId"] = _option.AndroidId,
                    ["app"] = _option.AndroidVending,
                    ["device_country"] = _option.DeviceCountry,
                    ["operatorCountry"] = _option.CountryCode,
                    ["lang"] = _option.Language,
                    ["sdk_version"] = _option.SdkVersion
                };
                using (var response = await PostAsync(_option.LoginUrl, new FormUrlEncodedContent(body)))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new GooglePlayApiLoginException(response);
                    }
                    var str = await response.Content.ReadAsStringAsync();
                    var loginResult = ResponseToObject(str);
                    if (!loginResult.ContainsKey("Auth") || string.IsNullOrWhiteSpace(loginResult["Auth"]))
                    {
                        throw new GooglePlayApiLoginException(response, "expected auth in server response");
                    }
                    _option.AuthToken = loginResult["Auth"];
                }
            }
            return _option.AuthToken;
        }

        #endregion
    }
}