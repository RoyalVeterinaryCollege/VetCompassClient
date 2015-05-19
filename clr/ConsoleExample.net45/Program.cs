using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VetCompass.Client;

namespace ConsoleExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new CodingSessionFactory(Guid.NewGuid(), "TI4hiVg2bTTR7+d/mqFo/7gMUiVZGOsC0JSMSVUI99VQO8cT+ImSfnPqPBPru1zdb12GbXB7C4W4Y300SUeR+w==", new Uri("https://vetcompass.herokuapp.com/api/1.0/session/"));
            var session = client.StartCodingSession(new CodingSubject { CaseNumber = "noel's testing case" });
            var results = session.QuerySynch(new VeNomQuery("rta"));
            Console.ReadKey();
        }

        private static void SelectionTesting()
        {
            var client = new CodingSessionFactory(Guid.NewGuid(), "not very secret", new Uri("http://192.168.1.199:5000/api/1.0/session/"));
            var session = client.StartCodingSession(new CodingSubject { CaseNumber = "noel's testing case" });
            var results = session.QuerySynch(new VeNomQuery("rta"));
            var post = session.RegisterSelection(new VetCompassCodeSelection("rta",results.Results.First().DataDictionaryId));

            Console.ReadKey();
        }

        void ThroughPutTest()
        {
            var client = new CodingSessionFactory(Guid.NewGuid(), "not very secret", new Uri("https://vetcompass.herokuapp.com/api/1.0/session/"));
            var session = client.StartCodingSession(new CodingSubject { CaseNumber = "noel's testing case" });
            var start = DateTime.Now;
            var results = new List<Task<VeNomQueryResponse>>();

            for (int i = 0; i < 10; i++)
            {
                results.Add(session.QueryAsync(new VeNomQuery("rta")));
            }
            var array = results.ToArray();
            Task.WaitAll(array);

            Console.WriteLine(DateTime.Now - start);
            Console.ReadKey();
        }

        public static string CreateServiceClientKey()
        {
            var hmac = new HMACSHA256();
            return Convert.ToBase64String(hmac.Key);
        }
    }
}
