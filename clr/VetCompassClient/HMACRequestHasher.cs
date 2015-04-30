using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;


namespace VetCompass.Client
{
    public class HMACRequestHasher
    {
        /// <summary>
        /// Header key for the request date
        /// </summary>
        public const string VetCompass_Date_Header = "vetcompass-request-date";

        /// <summary>
        /// Header key for HMAC authorisation 
        /// </summary>
        public const string VetCompass_HMAC_Header = "vetcompass-hmac-authorisation";

        /// <summary>
        /// Creates a signature of the request to be hashed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public string MakeSignatureForHashing(WebRequest request, Guid clientId) 
        {
            const string sep = "\n";
            var requestDate = request.Headers.Get(VetCompass_Date_Header);

            return request.Method + sep + clientId + request.ContentLength + sep + request.ContentType + sep + requestDate;
        }

        /// <summary>
        /// Adds the relevant HMAC headers to a request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="clientId"></param>
        /// <param name="sharedSecret"></param>
        public void HashRequest(WebRequest request, Guid clientId, string sharedSecret)
        {
            SetDateHeader(request);

            var sharedSecretKeyAsByteArray = ConvertToByteArray(sharedSecret);

            string unhashedSignature = MakeSignatureForHashing(request, clientId);
            byte[] byteEncoded = Encoding.UTF8.GetBytes(unhashedSignature);
            using (var hasher = new HMACSHA256(sharedSecretKeyAsByteArray))
            {
                var signature = Convert.ToBase64String(hasher.ComputeHash(byteEncoded));
                request.Headers.Add(VetCompass_HMAC_Header, string.Format("{0}:{1}", clientId, signature));
            }
        }

        private byte[] ConvertToByteArray(string sharedSecret)
        {
            var bytes = Encoding.UTF8.GetBytes(sharedSecret);
            string base64EncodedSharedSecretKey = Convert.ToBase64String(bytes);
            byte[] sharedSecretKeyAsByteArray = Convert.FromBase64String(base64EncodedSharedSecretKey);
            return sharedSecretKeyAsByteArray;
        }

        /// <summary>
        /// Sets the vetcompass reqesut date header
        /// </summary>
        /// <param name="request"></param>
        private void SetDateHeader(WebRequest request)
        {
            //storing the date the request was made reduces the window for replay attacks
            //http://stackoverflow.com/questions/44391/how-do-i-prevent-replay-attacks
            string date = DateTime.UtcNow.ToString("o"); //date format = ISO 8601, http://en.wikipedia.org/wiki/ISO_8601
            request.Headers.Add(VetCompass_Date_Header, date);
        }
    }
}
    