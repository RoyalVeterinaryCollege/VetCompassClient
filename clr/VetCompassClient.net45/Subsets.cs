namespace VetCompass.Client
{
    /// <summary>
    /// Literals for use when filtering results to subsets of the codes.  Use these literals to populate the FilterSubset property on the VeNomQuery dto
    /// </summary>
    public static class Subsets
    {
        //clinic
        public const int Diagnosis = 14;
        public const int MorbidityMortality = 7;
        public const int PresentingComplaint = 18;
        public const int PhysicalExamination = 17;
        public const int CoreHistory = 6;

        //signalment
        public const int Species = 2;
        public const int CanineBreed = 1; 
        public const int FelineBreed = 9;   
        public const int RabbitBreed = 3;
        
        //admin
        public const int ReasonForVisit = 16;
        public const int AdministrativeTask = 15;

        //tests and procedures
        public const int DiagnosticTest = 11;
        public const int Radiology = 10;
        public const int Ultrasonography = 5;
        public const int Procedure = 4;
        
        //meta - internal RVC use
        public const int Modelling = 13;
    }
}
