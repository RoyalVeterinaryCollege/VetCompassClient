using System;
using System.Net;

namespace VetCompass.Client
{
    public static class RequestFactory
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
            //todo:Time out
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
    }
}