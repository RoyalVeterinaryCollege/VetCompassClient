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
        /// <returns></returns>
        ICodingSession StartCodingSession(CodingSubject subject);

        /// <summary>
        /// Resumes a pre-registered coding session.  This assumes a session with that sessionId has been started with a previous call
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        ICodingSession ResumeCodingSession(CodingSubject subject, Guid sessionId);
    }

    /// <summary>
    /// A factory for starting or resuming clinical coding sessions with the VetCompass clinical coding web service.  Thread safe.
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

        public CodingSessionFactory(Guid clientId, string sharedSecret, Uri vetcompassWebserviceBase)
        {
            _clientId = clientId;
            _sharedSecret = sharedSecret;
            var expectedFormatForUri = vetcompassWebserviceBase.ToString().EndsWith("/")
                ? vetcompassWebserviceBase
                : new Uri(vetcompassWebserviceBase + "/");
            _vetcompassWebserviceBase = expectedFormatForUri;
        }

        public ICodingSession StartCodingSession(CodingSubject subject)
        {
            var session =  new CodingSession(_clientId, _sharedSecret, subject, _vetcompassWebserviceBase);
            session.Start();
            return session;
        }

        public ICodingSession ResumeCodingSession(CodingSubject subject, Guid sessionId)
        {
            var session =  new CodingSession(_clientId, _sharedSecret, subject, _vetcompassWebserviceBase);
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

        [Obsolete("For serialisor only")]
        public VeNomQuery(){ }

        public VeNomQuery(string searchExpression)
        {
            SearchExpression = searchExpression;
        }

        public string SearchExpression
        {
            get { return _searchExpression; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("SearchExpression");
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
        public string Name { get; set; }
        public string Subset { get; set; }
    }
}