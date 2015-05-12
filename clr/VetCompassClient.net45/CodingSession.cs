using System;
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
        /// Gets if the session has been started
        /// </summary>
        public bool IsStarted { get; private set; }

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
            if (IsStarted) return; //guard Start being called twice

            
            SessionId = Guid.NewGuid();
            _sessionAddress = new Uri(_vetcompassAddress + SessionId.ToString() + "/");

            //prepare the initial session creation post
            var request = CreateRequest(_sessionAddress);
            var requestBytes = RequestHelper.PreparePostRequest(request,Subject,_clientId,_sharedSecret);
           
            //webrequest asynch methods require caller to implement timeouts, set up cancellation tokens
            var cancellationTokenSource = new CancellationTokenSource();
            var ct = cancellationTokenSource.Token;

            //Asynchronously post the request
            var webTask = RequestHelper.PostAsynchronously(request, ct, requestBytes, HandleSessionCreationFailure);
            _sessionCreationTask = HonourTimeout(webTask, cancellationTokenSource);
            IsStarted = true;
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
            if (IsStarted) return; //guard being resumed after a start

            IsStarted = true;
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
        /// Informs the web service that the user has selected a code
        /// </summary>
        /// <param name="selection">VetCompassCodeSelection The details of their selection</param>
        /// <returns></returns>
        public Task<HttpWebResponse> RegisterSelection(VetCompassCodeSelection selection)
        {
            if(!IsStarted) throw new Exception("Coding session not started");
            if(IsFaulted) throw new Exception("Coding session is faulted");

            return _sessionCreationTask.FlatMapSuccess(_ => PostSelection(selection));
        }

        private Task<HttpWebResponse> PostSelection(VetCompassCodeSelection selection)
        {
            var selectionAddress = new Uri(_sessionAddress + "selection");

            //prepare the request
            var request = CreateRequest(selectionAddress);
            var requestBytes = RequestHelper.PreparePostRequest(request, selection, _clientId, _sharedSecret);

            //webrequest asynch methods require caller to implement timeouts, set up cancellation tokens
            var cancellationTokenSource = new CancellationTokenSource();
            var ct = cancellationTokenSource.Token;

            //Asynchronously post the request
            var webTask = RequestHelper.PostAsynchronously(request, ct, requestBytes, HandleSessionCreationFailure);
            return HonourTimeout(webTask, cancellationTokenSource).MapSuccess(response => (HttpWebResponse)response);
        }

        /// <summary>
        /// Implements a timeout on requests (if one was required)
        /// </summary>
        /// <param name="webTask"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns>A Task which will cancel after the requested timeout period</returns>
        private Task<WebResponse> HonourTimeout(Task<WebResponse> webTask, CancellationTokenSource cancellationTokenSource)
        {
            //set timeout if nec
            if (Timeout.HasValue)
            {
#if NET45
                cancellationTokenSource.CancelAfter(Timeout.Value);
#endif
                return webTask.CancelAfter(cancellationTokenSource, Timeout.Value);
            }
            return webTask;
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
        ///     Creates a HttpWebRequest to the uri and prepares all the common code
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private HttpWebRequest CreateRequest(Uri uri)
        {
            return RequestHelper.CreateRequest(uri, _cookies, _clientId);
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
        /// Gets if the session has been started
        /// </summary>
        bool IsStarted { get; }

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

        /// <summary>
        /// Informs the web service that the user has selected a code
        /// </summary>
        /// <param name="selection">VetCompassCodeSelection The details of their selection</param>
        /// <returns></returns>
        Task<HttpWebResponse> RegisterSelection(VetCompassCodeSelection selection);
    }
}
