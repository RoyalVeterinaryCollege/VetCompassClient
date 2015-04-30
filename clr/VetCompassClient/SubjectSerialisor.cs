using System;
using System.Text;

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
            
            //todo:hm, this is a bit horrible, can it be refactored?
            if (!String.IsNullOrWhiteSpace(_subject.CaseNumber)) sb.AppendFormat("{0}:{1}\n", "casenumber", _subject.CaseNumber);
            if (!String.IsNullOrWhiteSpace(_subject.BreedName))  sb.AppendFormat("{0}:{1}\n", "breedname", _subject.BreedName);
            if (!String.IsNullOrWhiteSpace(_subject.SpeciesName)) sb.AppendFormat("{0}:{1}\n", "speciesname", _subject.SpeciesName);
            if (!String.IsNullOrWhiteSpace(_subject.SpeciesName)) sb.AppendFormat("{0}:{1}\n", "speciesname", _subject.SpeciesName);


            sb.Append("}");
            return sb.ToString();
        }
    }
}