using System;

namespace VetCompass.Client
{
    /// <summary>
    /// Represents the patient subject being coded
    /// </summary>
    public class CodingSubject : IEquatable<CodingSubject>
    {
        /// <summary>
        /// Your system's unique identifier for the patient
        /// </summary>
        public string CaseNumber { get; set; }

        /// <summary>
        /// The VeNom Breed code of the patient if known
        /// </summary>
        public int? VeNomBreedCode { get; set; }

        /// <summary>
        /// The name of the breed in your system
        /// </summary>
        public string BreedName { get; set; }

        /// <summary>
        /// The VeNom Species code of the patient if known
        /// </summary>
        public int? VeNomSpeciesCode { get; set; }

        /// <summary>
        /// The name of the species in your system
        /// </summary>
        public string SpeciesName { get; set; }

        /// <summary>
        /// Is the patient female?  If false, then male
        /// </summary>
        public bool? IsFemale { get; set; }

        /// <summary>
        /// Has the patient been neutered?
        /// </summary>
        public bool? IsNeutered { get; set; }

        /// <summary>
        /// The approximated date of birth of the patient
        /// </summary>
        public DateTime? ApproximateDateOfBirth { get; set; }

        /// <summary>
        /// In the case of British patients, this is the postcode without the final two characters
        /// </summary>
        public string PartialPostCode { get; set; }

        /// <summary>
        /// A method for determining equivalence between two coding subjects
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(CodingSubject other)
        {
            return CaseNumber == other.CaseNumber && VeNomBreedCode == other.VeNomBreedCode &&
                 BreedName == other.BreedName && VeNomSpeciesCode == other.VeNomSpeciesCode &&
                 SpeciesName == other.SpeciesName
                 && IsFemale == other.IsFemale && IsNeutered == other.IsNeutered &&
                 ApproximateDateOfBirth == other.ApproximateDateOfBirth && PartialPostCode == other.PartialPostCode;
        }
    }
}