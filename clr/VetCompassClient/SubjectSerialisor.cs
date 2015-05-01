using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Util;

namespace VetCompass.Client
{
    public class SubjectSerialisor
    {
        private readonly CodingSubject _subject;

        public SubjectSerialisor(CodingSubject subject)
        {
            _subject = subject;
        }

        public string ToJson()
        {
            /*
        public string CaseNumber { get; set; }
        public int? VeNomBreedCode { get; set; }
        public string BreedName { get; set; }
        public int? VeNomSpeciesCode { get; set; }
        public string SpeciesName { get; set; }
        public bool? IsFemale { get; set; }
        public bool? IsNeutered { get; set; }
        public DateTime? ApproximateDateOfBirth { get; set; }
        public string PartialPostCode { get; set; }*/

            var sb = new StringBuilder();
            sb.Append("{");

            var dispatcher = MakeDispatcher();
            var properties = _subject.GetType().GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(_subject);
                if (value != null)
                    dispatcher[property.PropertyType](value, sb, property.Name);
            }
               
            sb.Append("}");
            return sb.ToString();
        }

        private Dictionary<Type, Func<object,StringBuilder, string, StringBuilder>> MakeDispatcher()
        {
            var dispatcher = new Dictionary<Type, Func<object, StringBuilder, string, StringBuilder>>
            {
                {typeof (string), (value, sb, propertyName) =>      Write(value as string, sb, propertyName)},
                {typeof (int?), (value, sb, propertyName) =>        Write(value as int?, sb, propertyName)},
                {typeof (bool?), (value, sb, propertyName) =>       Write(value as bool?, sb, propertyName)},
                {typeof (DateTime?), (value, sb, propertyName) =>   Write(value as DateTime?, sb, propertyName)}
            };

            return dispatcher;
        } 

      

        StringBuilder Write(string value, StringBuilder sb, string propertyName)
        {
            if (!String.IsNullOrWhiteSpace(value)) sb.AppendFormat("\"{0}\": \"{1}\",\n", propertyName.ToLower(), value.Replace("\"","\\\""));
            return sb;
        }

        StringBuilder Write(int? value, StringBuilder sb, string propertyName)
        {
            if (value != null) sb.AppendFormat("\"{0}\": {1},\n", propertyName.ToLower(), value);
            return sb;
        }

        StringBuilder Write(bool? value, StringBuilder sb, string propertyName)
        {
            if (value != null)
            {
                sb.AppendFormat("\"{0}\": {1},\n", propertyName.ToLower(), value.Value ? "true" : "false");
            }
            return sb;
        }

        StringBuilder Write(DateTime? value, StringBuilder sb, string propertyName)
        {
            if (value != null)
            {
                sb.AppendFormat("\"{0}\": \"{1}\",\n", propertyName.ToLower(), value.Value.ToString("o")); 
            }
            return sb;
        }
    }
}