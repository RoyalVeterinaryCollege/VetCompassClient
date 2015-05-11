#if NET35

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace VetCompass.Client
{
    /// <summary>
    /// This backports some methods on WebRequest available in .net 4.5 into .net 3.5
    /// </summary>
    public static class RequestHelper
    {

        /// <summary>
        ///     This back ports the asynch upload method from .net 4.5 to .net 3.5
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Task<Stream> GetRequestStreamAsync(this WebRequest request)
        {
            //function taking an completed asynch callback to create an upload stream, and returns that stream
            Func<IAsyncResult, Stream> f = asynchResult =>
            {
                var intialRequest = (WebRequest) asynchResult.AsyncState;
                return intialRequest.GetRequestStream();
            };

            return Task.Factory.FromAsync(request.BeginGetRequestStream, f, request);
        }

        /// <summary>
        ///     This back ports the asynch response method from .net 4.5 to .net 3.5
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Task<WebResponse> GetResponseAsync(this WebRequest request)
        {
            //function taking an completed asynch callback to create an upload stream, and returns that stream
            Func<IAsyncResult, WebResponse> f = asynchResult =>
            {
                var intialRequest = (WebRequest)asynchResult.AsyncState;
                return intialRequest.GetResponse();
            };

            return Task.Factory.FromAsync(request.BeginGetResponse, f, request);
        }
    }
}

#endif