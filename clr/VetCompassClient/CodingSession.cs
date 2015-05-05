using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace VetCompass.Client
{
    /// <summary>
    /// Handles calls to the VetCompass clinical coding web service.  ThreadSafe.
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
        ///     Gets the unique session id
        /// </summary>
        public Guid SessionId { get; private set; }

        /// <summary>
        ///     Gets the subject of the coding session
        /// </summary>
        public CodingSubject Subject { get; private set; }

        public VeNomQueryResponse QuerySynch(VeNomQuery query)
        {
            var queryAsync = QueryAsync(query);
            Task.WaitAny(queryAsync);
            return queryAsync.Result;
        }

        public Task<VeNomQueryResponse> QueryAsync(VeNomQuery query)
        {
            var queryTask = _sessionCreationTask.ContinueWith(task => Query(query));
            return queryTask.Unwrap();
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
            var requestBytes = Encoding.UTF8.GetBytes(content);
            request.ContentLength = requestBytes.Length;
           
            //HMAC hash the request
            var hmacHasher = new HMACRequestHasher();
            hmacHasher.HashRequest(request, _clientId, _sharedSecret, content);
            using (var stream = request.GetRequestStream())
            {
                stream.Write(requestBytes, 0, requestBytes.Length);
            }
            _sessionCreationTask = request.GetResponseAsync(); //todo:Error handling
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
            _sessionCreationTask = Task.FromResult(0); //http://stackoverflow.com/questions/13127177/if-my-interface-must-return-task-what-is-the-best-way-to-have-a-no-operation-imp
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
            return request;
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