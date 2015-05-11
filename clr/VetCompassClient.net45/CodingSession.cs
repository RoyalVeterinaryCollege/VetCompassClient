﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Xml;

namespace VetCompass.Client 
{
    /// <summary>
    ///     Handles calls to the VetCompass clinical coding web service.  ThreadSafe.
    /// </summary>
    public class CodingSession : ICodingSession
    {
        private readonly Guid _clientId;
        private readonly CookieContainer _cookies = new CookieContainer(); //this is how to share the session cookies (if any)
        private readonly string _sharedSecret;
        private readonly Uri _vetcompassAddress;
        private Uri _sessionAddress;
        private Task _sessionCreationTask;

        /// <summary>
        ///     Instantiates a coding session object
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="sharedSecret"></param>
        /// <param name="subject"></param>
        /// <param name="vetcompassAddress"></param>
        public CodingSession(Guid clientId, string sharedSecret, CodingSubject subject, Uri vetcompassAddress)
        {
            _vetcompassAddress = vetcompassAddress;
            _clientId = clientId;
            _sharedSecret = sharedSecret;
            Subject = subject;
        }

        /// <summary>
        ///     Gets if the coding session has faulted.  If true, it's no longer usable
        /// </summary>
        public bool IsFaulted { get; private set; }

        /// <summary>
        ///     If IsFaulted == true, this will hold the exception
        /// </summary>
        public AggregateException Exception { get; private set; }

        /// <summary>
        ///     If IsFaulted == true, this may contain an error message from the server (it can be null, ie when contacting the
        ///     server was impossible)
        /// </summary>
        public string ServerErrorMessage { get; private set; }

        /// <summary>
        ///     Gets the unique session id
        /// </summary>
        public Guid SessionId { get; private set; }

        /// <summary>
        ///     Gets the subject of the coding session
        /// </summary>
        public CodingSubject Subject { get; private set; }

        /// <summary>
        /// Gets or sets an optional timeout (milliseconds) for calls to the webservice.  Defaults to the .net WebRequest timeout
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        ///     Creates a new coding session on the webservice
        /// </summary>
        public void Start()
        {
            if (SessionId != default(Guid)) return; //guard Start being called twice

            SessionId = Guid.NewGuid();
            _sessionAddress = new Uri(_vetcompassAddress + SessionId.ToString() + "/");
            var request = CreateRequest(_sessionAddress);
            request.ContentType = "application/json";
            request.Method = WebRequestMethods.Http.Post;
            var content = JsonConvert.SerializeObject(Subject);
            //todo: the serialiser is putting in x:null, change to not entering key?
            var requestBytes = Encoding.UTF8.GetBytes(content);
            request.ContentLength = requestBytes.Length;

            //HMAC hash the request
            var hmacHasher = new HMACRequestHasher();
            hmacHasher.HashRequest(request, _clientId, _sharedSecret, content);

            //This is a pipeline of asynch tasks which create a session on the server or results in the coding session being faulted
           
            //webrequest asynch methods require caller to implement timeouts, set up cancellation tokens
            var cancellationTokenSource = new CancellationTokenSource();
            var ct = cancellationTokenSource.Token;

            var webTask = Task.Factory
                .StartNew(() => request.GetRequestStreamAsync(),ct) //GetRequestStreamAsync performs quite a lot of synchronous work, so call it inside a task: see https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.begingetrequeststream(v=vs.110).aspx
                .Unwrap()
                .MapSuccess(stream => HandleRequestPosting(request, stream, requestBytes),ct) //handle successful contact with server by writing request
                .FlatMapSuccess(postedRequest => postedRequest.GetResponseAsync(),ct) //handle successfully writing request by getting asynch response 
                .ActOnFailure(HandleSessionCreationFailure); //or handle any antecedent failure

            if (Timeout.HasValue)
            {
#if NET45
                cancellationTokenSource.CancelAfter(Timeout.Value);
#endif
                _sessionCreationTask = webTask.CancelAfter(cancellationTokenSource, Timeout.Value);
            }
        }

        /// <summary>
        /// Queries the VetCompass webservice synchronously.  This will block your thread, instead prefer QueryAsync.  This method will also throw an exception on a failure. 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public VeNomQueryResponse QuerySynch(VeNomQuery query)
        {
            var queryAsync = QueryAsync(query);
            Task.WaitAny(queryAsync); //will throw on a task fault
            return queryAsync.Result; 
        }

        /// <summary>
        /// Asynchronously queries the VetCompass webservice.  The task can be used for error detection/handling.  This call won't block or throw an exception.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public Task<VeNomQueryResponse> QueryAsync(VeNomQuery query)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var task = _sessionCreationTask.FlatMapSuccess(_ => Query(query), cancellationTokenSource.Token);

            if(Timeout.HasValue)
                return task.CancelAfter(cancellationTokenSource,Timeout.Value);

            return task;
        }

        /// <summary>
        ///     Configures this CodingSession to use a already created session
        /// </summary>
        /// <param name="sessionId"></param>
        public void Resume(Guid sessionId)
        {
            SessionId = sessionId;
            _sessionAddress = new Uri(_vetcompassAddress + sessionId.ToString() + "/");
            //no webservice call required for resumption, but set up a no-op task to continue from

#if NET45
            _sessionCreationTask = Task.FromResult(0);  //http://stackoverflow.com/questions/13127177/if-my-interface-must-return-task-what-is-the-best-way-to-have-a-no-operation-imp
#endif
#if NET35
            _sessionCreationTask = TaskHelper.FromResult(0);
#endif
        }

        /// <summary>
        ///     Handles task failure by faulting the session
        /// </summary>
        /// <param name="exception"></param>
        private void HandleSessionCreationFailure(AggregateException exception)
        {
            //fault the session
            Exception = exception.Flatten();
            IsFaulted = true;

            //try to get a server error message if possible
            var error = Exception.InnerExceptions.FirstOrDefault() as WebException;
            if (error != null)
            {
                var response = error.Response;

                if (response != null)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var sr = new StreamReader(stream, Encoding.UTF8))
                        {
                            ServerErrorMessage = sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes the request to the upload stream
        /// </summary>
        /// <param name="requestBytes"></param>
        /// <returns></returns>
        private WebRequest HandleRequestPosting(WebRequest request, Stream stream, byte[] requestBytes)
        {
            using (stream)
            {
                stream.Write(requestBytes, 0, requestBytes.Length);
            }
            return request;
        }

        /// <summary>
        ///     Creates a HttpWebRequest to the uri and prepares all the common code
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private HttpWebRequest CreateRequest(Uri uri)
        {
            return RequestFactory.CreateRequest(uri, _cookies, _clientId);
        }

        /// <summary>
        /// This does the actual query call to the server
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private Task<VeNomQueryResponse> Query(VeNomQuery query)
        {
            var encoded = HttpUtility.UrlEncode(query.SearchExpression);
            var request = CreateRequest(new Uri(_sessionAddress + "search/" + encoded));
            request.Method = WebRequestMethods.Http.Get;
            request.Accept = "application/json";
            var task = request.GetResponseAsync();
            return task.MapSuccess(DeserialiseQueryReponse);
        }

        /// <summary>
        ///     Deserialises the web service's query response
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private VeNomQueryResponse DeserialiseQueryReponse(WebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    var responseContent = sr.ReadToEnd();
                    return JsonConvert.DeserializeObject<VeNomQueryResponse>(responseContent);
                }
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <remarks>Use this interface to mock out your code for testing etc</remarks>
    public interface ICodingSession
    {
        /// <summary>
        ///     Gets the unique session id
        /// </summary>
        Guid SessionId { get; }

        /// <summary>
        ///     Gets the subject of the coding session
        /// </summary>
        CodingSubject Subject { get; }

        /// <summary>
        ///     Gets if the coding session has faulted.  If true, it's no longer usable
        /// </summary>
        bool IsFaulted { get; }

        /// <summary>
        ///     If IsFaulted == true, this will hold the exception
        /// </summary>
        AggregateException Exception { get; }

        /// <summary>
        ///     If IsFaulted == true, this may contain an error message from the server (it can be null, ie when contacting the
        ///     server was impossible)
        /// </summary>
        string ServerErrorMessage { get; }

        /// <summary>
        /// Gets or sets an optional timeout (milliseconds) for calls to the webservice.  Defaults to the .net WebRequest timeout
        /// </summary>
        int? Timeout { get; set; }

        /// <summary>
        ///     Queries the web service synchronously
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        VeNomQueryResponse QuerySynch(VeNomQuery query);

        /// <summary>
        ///     Queries the web service asynchronously
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<VeNomQueryResponse> QueryAsync(VeNomQuery query);

        /// <summary>
        ///     Creates a new coding session on the webservice
        /// </summary>
        void Start();

        /// <summary>
        ///     Configures this CodingSession to use a already created session
        /// </summary>
        /// <param name="sessionId"></param>
        void Resume(Guid sessionId);
    }
}
