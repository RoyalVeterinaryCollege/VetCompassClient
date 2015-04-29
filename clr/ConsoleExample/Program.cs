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
            var requestId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            var request = WebRequest.Create("http://venomcoding.herokuapp.com/api/1.0/session/" + requestId);
            request.ContentType = "string/json";
            var content = @"{""test"" = 1 }";
            request.ContentLength = Encoding.UTF8.GetBytes(content).Length;
            request.Method = "POST";
            var hmacHasher = new HMACRequestHasher();
            hmacHasher.HashRequest(request,clientId,"sjdfhsdf7tsf6ts6fts67ftsd67f6s7f67sdft");


        }
    }
}
