using System;
using System.Collections.Generic;

namespace VetCompass.Client
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Interface allows testing / mocking etc</remarks>
    public interface ICodingSessionFactory
    {
        /// <summary>
        /// Starts a new coding session which is registered on the web service
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        ICodingSession StartCodingSession(CodingSubject subject, int? timeoutMilliseconds = null);

        /// <summary>
        /// Resumes a pre-registered coding session.  This assumes a session with that sessionId has been started with a previous call
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="sessionId"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        ICodingSession ResumeCodingSession(CodingSubject subject, Guid sessionId, int? timeoutMilliseconds = null);
    }

    /// <summary>
    /// A factory for starting or resuming clinical coding sessions with the VetCompass clinical coding web service.  Thread safe. Can be a singleton.
    /// </summary>
    public class CodingSessionFactory : ICodingSessionFactory
    {
        private readonly Guid _clientId;
        private readonly string _sharedSecret;
        private readonly Uri _vetcompassWebserviceBase;

        public CodingSessionFactory()
        {
            //todo: get input from config
        }

        /// <summary>
        /// Instantiates the coding session factory.  This class is threadsafe and can be used as a singleton.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="sharedSecret"></param>
        /// <param name="vetcompassWebserviceBase"></param>
        public CodingSessionFactory(Guid clientId, string sharedSecret, Uri vetcompassWebserviceBase)
        {
            _clientId = clientId;
            _sharedSecret = sharedSecret;
            var expectedFormatForUri = vetcompassWebserviceBase.ToString().EndsWith("/")
                ? vetcompassWebserviceBase
                : new Uri(vetcompassWebserviceBase + "/");
            _vetcompassWebserviceBase = expectedFormatForUri;
        }

        /// <summary>
        /// Starts a new coding session on the web service
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public ICodingSession StartCodingSession(CodingSubject subject, int? timeoutMilliseconds = null)
        {
            var session = new CodingSession(_clientId, _sharedSecret, subject, _vetcompassWebserviceBase)
            {
                Timeout = timeoutMilliseconds
            };
            session.Start();
            return session;
        }

        /// <summary>
        /// Resumes a pre-started coding session.  This assumes the session been started with a previous call to Start and that the same sessionId is used to resume.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="sessionId"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public ICodingSession ResumeCodingSession(CodingSubject subject, Guid sessionId, int? timeoutMilliseconds = null)
        {
            var session = new CodingSession(_clientId, _sharedSecret, subject, _vetcompassWebserviceBase)
            {
                Timeout = timeoutMilliseconds
            };
            session.Resume(sessionId);
            return session;
        }
    }

    /// <summary>
    /// A DTO representing a VeNom query
    /// </summary>
    public class VeNomQuery
    {
        private string _searchExpression;

        /// <summary>
        /// Don't use this constructor
        /// </summary>
        [Obsolete("For serialisor only")]
        public VeNomQuery(){ }

        /// <summary>
        /// Instantiates a new VeNomQuery
        /// </summary>
        /// <param name="searchExpression"></param>
        public VeNomQuery(string searchExpression)
        {
            SearchExpression = searchExpression;
        }

        /// <summary>
        /// Gets or sets the Search Expression
        /// </summary>
        public string SearchExpression
        {
            get { return _searchExpression; }
            set
            {
                if (String.IsNullOrEmpty(value) || string.IsNullOrEmpty(value.Trim())) throw new ArgumentNullException("SearchExpression");
                _searchExpression = value;
            }
        }

        /// <summary>
        ///     How many hits to skip at the beginning of the results.
        /// </summary>
        /// <remarks>defaults to 0 on server</remarks>
        public int? Skip { get; set; }

        /// <summary>
        ///     How many hits to take
        /// </summary>
        /// <remarks>Defaults to 10 on the server.  Min = 0, Max = 100</remarks>
        public int? Take { get; set; } //defaults to 10 on server

        /// <summary>
        ///     The subsets to filter the results by
        /// </summary>
        public HashSet<int> FilterSubset { get; set; } //defaults to all except 'Modelling'
    }

    /// <summary>
    ///     A dto representing the web-service's response to a session query
    /// </summary>
    public class VeNomQueryResponse
    {
        /// <summary>
        ///     The original query request
        /// </summary>
        public VeNomQuery Query { get; set; }

        /// <summary>
        ///     The matches found for the query
        /// </summary>
        public List<VetCompassCode> Results { get; set; }
    }

    /// <summary>
    ///     A dto representing a code in VetCompass
    /// </summary>
    public class VetCompassCode
    {
        //todo:rename to VeNomId?
        public int DataDictionaryId { get; set; }

        /// <summary>
        /// Gets or sets the name of the code
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the subset to which the code belongs
        /// </summary>
        public string Subset { get; set; }
    }

    /// <summary>
    /// A dto representing a code selected by a user
    /// </summary>
    public class VetCompassCodeSelection
    {
        /// <summary>
        /// Instantiates a VetCompassCodeSelection
        /// </summary>
        /// <param name="searchExpression"></param>
        /// <param name="veNomId"></param>
        public VetCompassCodeSelection(string searchExpression, int veNomId)
        {
            
            if (string.IsNullOrEmpty(searchExpression)) throw new ArgumentNullException("searchExpression");
            SearchExpression = searchExpression;
            VeNomId = veNomId;
        }

        /// <summary>
        /// The string that was in the search box when the user made the selection
        /// </summary>
        public string SearchExpression { get; private set; }

        /// <summary>
        /// The Id of the code they selected
        /// </summary>
        public int VeNomId { get; set; }
    }
}