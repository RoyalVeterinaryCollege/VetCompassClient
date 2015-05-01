using System.Text;

namespace VetCompass.Client
{
    /// <summary>
    ///     A json serialisor for the CodingSubject dto.
    /// </summary>
    /// <remarks>Rolling your own serialisation isn't a great idea, but doing it avoids having to take a 3rd party dependency</remarks>
    public class SubjectSerialisor
    {
        private readonly CodingSubject _subject;

        public SubjectSerialisor(CodingSubject subject)
        {
            _subject = subject;
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{");

            var properties = _subject.GetType().GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(_subject);
                if (value != null)
                    Dispatcher.SerialiseValue[property.PropertyType](value, sb, property.Name);
            }

            sb.Append("}");
            return sb.ToString();
        }
    }
}