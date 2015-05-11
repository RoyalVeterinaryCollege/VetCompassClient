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
            Func<IAsyncResult, WebRequest> f = asynchResult =>
            {
                var intialRequest = (WebRequest) asynchResult.AsyncState;
                return intialRequest;
            };

            return Task.Factory
                .FromAsync(request.BeginGetRequestStream, f, request)
                .MapSuccess(initialRequest => initialRequest.GetRequestStream()); //do the request inside a task to prevent exceptions taking down the appdomain
        }

        /// <summary>
        ///     This back ports the asynch response method from .net 4.5 to .net 3.5
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Task<WebResponse> GetResponseAsync(this WebRequest request)
        {
            //function taking an completed asynch callback which represents a completed request and gets that request
            Func<IAsyncResult, WebRequest> f = asynchResult =>
            {
                var intialRequest = (WebRequest)asynchResult.AsyncState;
                return intialRequest;
            };

            return Task.Factory
                .FromAsync(request.BeginGetResponse, f, request)
                .MapSuccess(completedRequest => request.GetResponse()); //second task gets the response.  It's done in a second task as GetResponse can actually throw an error, and we want this to happen inside a task (rather than in the callback which will take down the appdomain!)
        }
    }
}

#endif