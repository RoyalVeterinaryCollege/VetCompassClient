using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VetCompass.Client
{
    public static class RequestHelper
    {
        /// <summary>
        ///     Creates a HttpWebRequest to the uri and prepares all the common code
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cookies"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static HttpWebRequest CreateRequest(Uri uri, CookieContainer cookies, Guid clientId)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = true;
            request.CookieContainer = cookies;
            SetDateHeader(request);
            SetClientHeader(request, clientId);
            return request;
        }

        public static void SetClientHeader(HttpWebRequest request, Guid clientId)
        {
            request.Headers.Add(Constants.VetCompass_clientid_Header, clientId.ToString());
        }

        /// <summary>
        ///     Sets the vetcompass request date header
        /// </summary>
        /// <param name="request"></param>
        public static void SetDateHeader(WebRequest request)
        {
            //storing the date the request was made reduces the window for replay attacks
            //http://stackoverflow.com/questions/44391/how-do-i-prevent-replay-attacks
            var date = DateTime.UtcNow.ToString("o"); //date format = ISO 8601, http://en.wikipedia.org/wiki/ISO_8601
            request.Headers.Add(Constants.VetCompass_Date_Header, date);
        }

        public static byte[] PreparePostRequest(WebRequest request, object body, Guid clientId, string sharedSecret)
        {
            request.ContentType = "application/json";
            request.Method = WebRequestMethods.Http.Post;
            var contentString = JsonConvert.SerializeObject(body);
            var requestBytes = Encoding.UTF8.GetBytes(contentString);
            request.ContentLength = requestBytes.Length;

            //HMAC hash the request
            var hmacHasher = new HMACRequestHasher();
            hmacHasher.HashRequest(request, clientId, sharedSecret, contentString);
            return requestBytes;
        }

        /// <summary>
        /// This is a pipeline of asynch tasks which post the web request 
        /// </summary>
        /// <param name="request">WebRequest the prepared request</param>
        /// <param name="ct">CancellationToken a cancellation token for aborting the request</param>
        /// <param name="requestBytes">byte[] the UTF encoded body of the request</param>
        /// <param name="exceptionHandler">an exception handler to invoke in the case of error</param>
        /// <returns></returns>
        public static Task<WebResponse> PostAsynchronously(WebRequest request, CancellationToken ct, byte[] requestBytes, Action<AggregateException> exceptionHandler)
        {
            var webTask = Task.Factory
                .StartNew(() => request.GetRequestStreamAsync(), ct) //GetRequestStreamAsync performs quite a lot of synchronous work, so call it inside a task: see https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.begingetrequeststream(v=vs.110).aspx
                .Unwrap()
                .MapSuccess(stream => HandleRequestPosting(request, stream, requestBytes), ct) //handle successful contact with server by writing request
                .FlatMapSuccess(postedRequest => postedRequest.GetResponseAsync(), ct) //handle successfully writing request by getting asynch response 
                .ActOnFailure(exceptionHandler); //or handle any antecedent failure

            return webTask;
        }

        /// <summary>
        /// Writes the request to the upload stream
        /// </summary>
        /// <param name="requestBytes"></param>
        /// <returns></returns>
        private static WebRequest HandleRequestPosting(WebRequest request, Stream stream, byte[] requestBytes)
        {
            using (stream)
            {
                stream.Write(requestBytes, 0, requestBytes.Length);
            }
            return request;
        }
    }
}