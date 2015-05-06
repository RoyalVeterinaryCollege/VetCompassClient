using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace VetCompass.Client
{
    /// <summary>
    ///     Handles calls to the VetCompass clinical coding web service.  ThreadSafe.
    /// </summary>
    public class CodingSession : ICodingSession
    {
        private readonly Guid _clientId;
        private readonly CookieContainer _cookies = new CookieContainer(); //this is how to share the session
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
        /// Queries the VetCompass webservice synchronously.  This will block your thread, instead prefer QueryAsync.  This method will also throw on exception. 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public VeNomQueryResponse QuerySynch(VeNomQuery query)
        {
            var queryAsync = QueryAsync(query);
            Task.WaitAny(queryAsync);
            return queryAsync.Result; //will throw on a task fault
        }

        /// <summary>
        /// Asynchronously queries the VetCompass webservice.  The task can be used for error detection/handling.  This call won't block.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public Task<VeNomQueryResponse> QueryAsync(VeNomQuery query)
        {
            return _sessionCreationTask.FlatMapSuccess(_ => Query(query));
        }

        /// <summary>
        ///     Creates a new coding session on the webservice
        /// </summary>
        public void Start()
        {
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

            //This is a pipeline of asynch tasks
            _sessionCreationTask = request
                .GetRequestStreamAsync() //the upload request stream actually tries to contact the server, so asynch from here
                .MapSuccess(t => HandleRequestPosting(requestBytes)) //handle successful contact with server by writing request
                .MapSuccess(t => request.GetResponseAsync()) //handle successful request write by getting asynch response 
                .ActOnFailure(HandleSessionCreationFailure); //or handle any failure at any point
        }

        /// <summary>
        ///     Configures this CodingSession to use a already created session
        /// </summary>
        /// <param name="sessionId"></param>
        public void Resume(Guid sessionId)
        {
            SessionId = sessionId;
            _sessionAddress = new Uri(_vetcompassAddress + sessionId.ToString() + "/");
            //no call required to web service, but set up a no-op task to continue from
            _sessionCreationTask = Task.FromResult(0);
            //http://stackoverflow.com/questions/13127177/if-my-interface-must-return-task-what-is-the-best-way-to-have-a-no-operation-imp
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
        private Action<Task<Stream>> HandleRequestPosting(byte[] requestBytes)
        {
            return task =>
            {
                using (var stream = task.Result)
                {
                    stream.Write(requestBytes, 0, requestBytes.Length);
                }
            };
        }

        /// <summary>
        ///     Creates a HttpWebRequest to the uri and prepares all the common code
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private HttpWebRequest CreateRequest(Uri uri)
        {
            //todo:Time out
            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.KeepAlive = true;
            request.CookieContainer = _cookies;
            SetDateHeader(request);
            SetClientHeader(request);
            return request;
        }

        private void SetClientHeader(HttpWebRequest request)
        {
            request.Headers.Add(Constants.VetCompass_clientid_Header, _clientId.ToString());
        }

        /// <summary>
        ///     Sets the vetcompass request date header
        /// </summary>
        /// <param name="request"></param>
        private void SetDateHeader(WebRequest request)
        {
            //storing the date the request was made reduces the window for replay attacks
            //http://stackoverflow.com/questions/44391/how-do-i-prevent-replay-attacks
            var date = DateTime.UtcNow.ToString("o"); //date format = ISO 8601, http://en.wikipedia.org/wiki/ISO_8601
            request.Headers.Add(Constants.VetCompass_Date_Header, date);
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
            return task.ContinueWith(t => DeserialiseQueryReponse(t.Result));
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