﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace VetCompass.Client
{
    public class VetCompassWebservicesClient
    {
        private readonly Guid _clientId;
        private readonly string _sharedSecret;
        private readonly Uri _vetcompassWebserviceBase;

        public VetCompassWebservicesClient()
        {
            //todo: get input from config
            
        }
        public VetCompassWebservicesClient(Guid clientId, string sharedSecret, Uri vetcompassWebserviceBase)
        {
            _clientId = clientId;
            _sharedSecret = sharedSecret;
            var expectedFormatForUri = vetcompassWebserviceBase.ToString().EndsWith("/")
              ? vetcompassWebserviceBase
              : new Uri(vetcompassWebserviceBase + "/");
            _vetcompassWebserviceBase = expectedFormatForUri;
        }

        public CodingSession StartCodingSession(CodingSubject subject)
        {
            return ResumeCodingSession(subject, Guid.NewGuid());
        }

        public CodingSession ResumeCodingSession(CodingSubject subject, Guid sessionId)
        {
            return new CodingSession(_clientId, _sharedSecret, sessionId, subject, _vetcompassWebserviceBase);
        }
    }

    public class CodingSession : ICodingSession
    {
        private readonly Guid _clientId;
        private readonly string _sharedSecret;
        private readonly Uri _sessionAddress;
        private Task<WebResponse> _sessionCreationTask;

        /// <summary>
        /// Gets the unique session id
        /// </summary>
        public Guid SessionId { get; private set; }

        /// <summary>
        /// Gets the subject of the coding session
        /// </summary>
        public CodingSubject Subject { get; private set; }

        /// <summary>
        /// Instantiates the coding session object and starts a coding session with the VetCompass webservice
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="sharedSecret"></param>
        /// <param name="sessionId"></param>
        /// <param name="subject"></param>
        /// <param name="vetcompassAddress"></param>
        public CodingSession(Guid clientId, string sharedSecret, Guid sessionId, CodingSubject subject, Uri vetcompassAddress)
        {
            _clientId = clientId;
            _sharedSecret = sharedSecret;
            SessionId = sessionId;
            Subject = subject;
            _sessionAddress = new Uri(vetcompassAddress + sessionId.ToString() + "/");
            //todo:what about a web-based system, they would not want to create the session each time?
            CreateSession();
        }

        private void CreateSession()
        {
            //todo:time out
            var request = WebRequest.Create(_sessionAddress);
            request.ContentType = "string/json";
            request.Method = "POST";
            var content = JsonConvert.SerializeObject(Subject);
            var requestBytes = Encoding.UTF8.GetBytes(content);
            request.ContentLength = requestBytes.Length;
            
            var hmacHasher = new HMACRequestHasher();
            hmacHasher.HashRequest(request, _clientId, _sharedSecret);
            using (var stream = request.GetRequestStream())
            {
                stream.Write(requestBytes,0,requestBytes.Length);
            }
            _sessionCreationTask = request.GetResponseAsync();
        }
   
        //todo queryexpression dto, ie skip/take, filters, etc
        public QueryResponse QuerySynch(VeNomQuery query)
        {
            var queryAsync = QueryAsync(query);
            Task.WaitAny(queryAsync);
            return queryAsync.Result;
        }

        public Task<QueryResponse> QueryAsync(VeNomQuery query)
        {
            return _sessionCreationTask.ContinueWith(task => Query(query));
        }

        private QueryResponse Query(VeNomQuery query)
        {
            //todo:time outs
            var encoded = HttpUtility.HtmlEncode(query.QueryExpression);
            var request = WebRequest.Create(_sessionAddress + "search/" + encoded);
            request.Method = "GET";
            return DeserialiseQueryReponse(request.GetResponse());
        }

        /// <summary>
        /// Deserialises the web service's query response
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private QueryResponse DeserialiseQueryReponse(WebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    var responseContent = sr.ReadToEnd();
                    return JsonConvert.DeserializeObject<QueryResponse>(responseContent);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Use this interface to mock out your code for testing etc</remarks>
    public interface ICodingSession
    {
        /// <summary>
        /// Queries the web service synchronously
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        QueryResponse QuerySynch(VeNomQuery query);

        /// <summary>
        /// Queries the web service asynchronously
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<QueryResponse> QueryAsync(VeNomQuery query);

        /// <summary>
        /// Gets the unique session id
        /// </summary>
        Guid SessionId { get; }

        /// <summary>
        /// Gets the subject of the coding session
        /// </summary>
        CodingSubject Subject { get; }
    }

    public class VeNomQuery 
    {
        public string QueryExpression { get; private set; }

        public VeNomQuery(string queryExpression)
        {
            if(String.IsNullOrWhiteSpace(queryExpression)) throw new ArgumentNullException("queryExpression");
            QueryExpression = queryExpression;
        }

        public int? Skip { get; set; } //defaults to 0 on server

        public int? Take { get; set; } //defaults to 10 on server

        public HashSet<int> FilterSet { get; set; } //defaults to all except 'Modelling'
    }

    /// <summary>
    /// A dto representing the web-service's response to a session query
    /// </summary>
    public class QueryResponse  
    {
        public string QueryExpression { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public HashSet<int> FilterSubset { get; set; }
        public List<VetCompassCode> Results { get; set; }


    }

    public class VetCompassCode
    {
        public int DataDictionaryId { get; set; }
        public string Name { get; set; }
        public string Subset { get; set; } 
    }
}
