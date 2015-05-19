using System;

namespace VetCompass.Client
{
    /// <summary>
    /// A dto representing a code selected by a user
    /// </summary>
    public class VetCompassCodeSelection
    {
        /// <summary>
        /// Instantiates a VetCompassCodeSelection
        /// </summary>
        /// <param name="searchExpression"></param>
        /// <param name="veNomId"></param>
        public VetCompassCodeSelection(string searchExpression, int veNomId)
        {
            
            if (string.IsNullOrEmpty(searchExpression)) throw new ArgumentNullException("searchExpression");
            SearchExpression = searchExpression;
            VeNomId = veNomId;
        }

        /// <summary>
        /// The string that was in the search box when the user made the selection
        /// </summary>
        public string SearchExpression { get; private set; }

        /// <summary>
        /// The Id of the code they selected
        /// </summary>
        public int VeNomId { get; set; }
    }
}