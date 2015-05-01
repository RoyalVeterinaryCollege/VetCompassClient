using System;
using System.Collections.Generic;
using System.Text;

namespace VetCompass.Client
{
    public static class Dispatcher
    {
        /// <summary>
        /// This is a dispatcher for serialising various .net types
        /// </summary>
        /// <returns></returns>
        public static readonly Dictionary<Type, Func<object, StringBuilder, string, StringBuilder>>  SerialiseValue = MakeDispatcher();

     
        private static Dictionary<Type, Func<object, StringBuilder, string, StringBuilder>> MakeDispatcher()
        {
            var dispatcher = new Dictionary<Type, Func<object, StringBuilder, string, StringBuilder>>
            {
                {typeof (string), (value, sb, propertyName) => Write(value as string, sb, propertyName)},
                {typeof (int?), (value, sb, propertyName) => Write(value as int?, sb, propertyName)},
                {typeof (bool?), (value, sb, propertyName) => Write(value as bool?, sb, propertyName)},
                {typeof (DateTime?), (value, sb, propertyName) => Write(value as DateTime?, sb, propertyName)}
            };

            return dispatcher;
        }

        private static StringBuilder Write(string value, StringBuilder sb, string propertyName)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendFormat("\"{0}\": \"{1}\",\n", propertyName.ToLower(), value.Replace("\"", "\\\""));
            return sb;
        }

        private static StringBuilder Write(int? value, StringBuilder sb, string propertyName)
        {
            if (value != null) sb.AppendFormat("\"{0}\": {1},\n", propertyName.ToLower(), value);
            return sb;
        }

        private static StringBuilder Write(bool? value, StringBuilder sb, string propertyName)
        {
            if (value != null)
            {
                sb.AppendFormat("\"{0}\": {1},\n", propertyName.ToLower(), value.Value ? "true" : "false");
            }
            return sb;
        }

        private static StringBuilder Write(DateTime? value, StringBuilder sb, string propertyName)
        {
            if (value != null)
            {
                sb.AppendFormat("\"{0}\": \"{1}\",\n", propertyName.ToLower(), value.Value.ToString("o"));
            }
            return sb;
        }
    }
}