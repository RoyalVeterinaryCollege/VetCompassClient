using System;

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
}