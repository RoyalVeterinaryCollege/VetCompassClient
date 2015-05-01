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

            var client = new VetCompassWebservicesClient(Guid.NewGuid(), "not very secret", new Uri("https://venomcoding.herokuapp.com/api/1.0/session/"));
            var session = client.StartCodingSession(new CodingSubject {CaseNumber = "noel's testing case"});

            var query = session.QueryAsync(new VeNomQuery("rta"));
            Task.WaitAll(query);
           

        }
    }
}
