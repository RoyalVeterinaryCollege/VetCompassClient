using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

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
              ? _vetcompassWebserviceBase
              : new Uri(_vetcompassWebserviceBase + "/");
            _vetcompassWebserviceBase = expectedFormatForUri;
        }

        public CodingSession StartCodingSession(CodingSubject subject)
        {
            return StartCodingSession(subject, Guid.NewGuid());
        }

        public CodingSession StartCodingSession(CodingSubject subject, Guid sessionId)
        {
            return new CodingSession(_clientId, _sharedSecret, sessionId, subject, _vetcompassWebserviceBase);
        }
    }

    /// <summary>
    /// Represents the patient subject being coded
    /// </summary>
    public class CodingSubject
    {
        /// <summary>
        /// Your system's unique identifier for the patient
        /// </summary>
        public string CaseNumber { get; set; }

        /// <summary>
        /// The VeNom Breed code of the patient if known
        /// </summary>
        public int? VeNomBreedCode { get; set; }

        /// <summary>
        /// The name of the breed in your system
        /// </summary>
        public string BreedName { get; set; }

        /// <summary>
        /// The VeNom Species code of the patient if known
        /// </summary>
        public int? VeNomSpeciesCode { get; set; }

        /// <summary>
        /// The name of the species in your system
        /// </summary>
        public string SpeciesName { get; set; }

        /// <summary>
        /// Is the patient female?  If false, then male
        /// </summary>
        public bool? IsFemale { get; set; }

        /// <summary>
        /// Has the patient been neutered?
        /// </summary>
        public bool? IsNeutered { get; set; }

        /// <summary>
        /// The approximated date of birth of the patient
        /// </summary>
        public DateTime? ApproximateDateOfBirth { get; set; }

        /// <summary>
        /// In the case of British patients, this is the postcode without the final two characters
        /// </summary>
        public string PartialPostCode { get; set; }
    }

    public class CodingSession  
    {
        private readonly Guid _clientId;
        private readonly string _sharedSecret;
        private readonly Uri _sessionAddress;
        private Task<WebResponse> _runningTask;

        /// <summary>
        /// Gets the unique session id
        /// </summary>
        public Guid SessionId { get; private set; }

        /// <summary>
        /// Gets the subject of the coding session
        /// </summary>
        public CodingSubject Subject { get; private set; }


        public CodingSession(Guid clientId, string sharedSecret, Guid sessionId, CodingSubject subject, Uri vetcompassAddress)
        {
            _clientId = clientId;
            _sharedSecret = sharedSecret;
            SessionId = sessionId;
            Subject = subject;
            _sessionAddress = new Uri(vetcompassAddress + sessionId.ToString() + "/");
        }

        public void Start()
        {
            var request = WebRequest.Create(_sessionAddress);
            request.ContentType = "string/json";
            var content = GetSubjectAsString();
            request.ContentLength = Encoding.UTF8.GetBytes(content).Length;
            request.Method = "POST";
            var hmacHasher = new HMACRequestHasher();
            hmacHasher.HashRequest(request, _clientId, _sharedSecret);
            _runningTask = request.GetResponseAsync();
        }

        public QueryResponse Query(string queryExpression)
        {
            throw new NotImplementedException();
        }

        private string GetSubjectAsString()
        {
            throw new NotImplementedException();
        }
    }

    public class QueryResponse  
    {
    }
}
