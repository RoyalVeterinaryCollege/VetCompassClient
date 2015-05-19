namespace VetCompass.Client
{
    /// <summary>
    ///     A dto representing a code in VetCompass
    /// </summary>
    public class VetCompassCode
    {
        /// <summary>
        /// Gets or sets the VeNomId of the code
        /// </summary>
        public int VeNomId { get; set; }

        /// <summary>
        /// Gets or sets the name of the code
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the subset to which the code belongs
        /// </summary>
        public string Subset { get; set; }
    }
}