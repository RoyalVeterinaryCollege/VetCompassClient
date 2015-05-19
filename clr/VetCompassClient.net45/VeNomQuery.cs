using System;
using System.Collections.Generic;

namespace VetCompass.Client
{
    /// <summary>
    /// A DTO representing a VeNom query
    /// </summary>
    public class VeNomQuery
    {
        private string _searchExpression;

        /// <summary>
        /// Don't use this constructor
        /// </summary>
        [Obsolete("For serialisor only")]
        public VeNomQuery(){ }

        /// <summary>
        /// Instantiates a new VeNomQuery
        /// </summary>
        /// <param name="searchExpression"></param>
        public VeNomQuery(string searchExpression)
        {
            SearchExpression = searchExpression;
        }

        /// <summary>
        /// Gets or sets the Search Expression
        /// </summary>
        public string SearchExpression
        {
            get { return _searchExpression; }
            set
            {
                if (String.IsNullOrEmpty(value) || string.IsNullOrEmpty(value.Trim())) throw new ArgumentNullException("SearchExpression");
                _searchExpression = value;
            }
        }

        /// <summary>
        ///     How many hits to skip at the beginning of the results.
        /// </summary>
        /// <remarks>defaults to 0 on server</remarks>
        public int? Skip { get; set; }

        /// <summary>
        ///     How many hits to take
        /// </summary>
        /// <remarks>Defaults to 10 on the server.  Min = 0, Max = 100</remarks>
        public int? Take { get; set; } //defaults to 10 on server

        /// <summary>
        ///     The subsets to filter the results by
        /// </summary>
        public HashSet<int> FilterSubset { get; set; } //defaults to all except 'Modelling'
    }
}