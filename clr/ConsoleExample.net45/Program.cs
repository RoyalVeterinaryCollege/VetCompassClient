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
            //var key = CreateServiceClientKey();
            //var id = Guid.NewGuid();

            var client = new CodingSessionFactory(Guid.Parse("6219abd9-b229-458c-baa0-2fc80763193e"), "ai5VdBnKx8GD1HT7fo6WTkHTKqGsBbhfcXX0cOHnuj93mjDrtKbgDjbqLNDfZoJeRVRD3pIspg3Scydm2Gx3CQ==", new Uri("https://vetcompass.herokuapp.com/api/1.0/session/"));
            var session = client.StartCodingSession(new CodingSubject { CaseNumber = "noel's testing case" });
            var results = session.QuerySynch(new VeNomQuery("rta"));
            Console.ReadKey();
        }

        private static void SelectionTesting()
        {
            var client = new CodingSessionFactory(Guid.NewGuid(), "not very secret", new Uri("http://192.168.1.199:5000/api/1.0/session/"));
            var session = client.StartCodingSession(new CodingSubject { CaseNumber = "noel's testing case" });
            var results = session.QuerySynch(new VeNomQuery("rta"));
            var post = session.RegisterSelection(new VetCompassCodeSelection("rta",results.Results.First().VeNomId));

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
