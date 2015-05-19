using System.Collections.Generic;

namespace VetCompass.Client
{
    /// <summary>
    ///     A dto representing the web-service's response to a session query
    /// </summary>
    public class VeNomQueryResponse
    {
        /// <summary>
        ///     The original query request
        /// </summary>
        public VeNomQuery Query { get; set; }

        /// <summary>
        /// Gets a search expression suggested by the service to use instead of the user's original search expression.  If null, there is no suggestion
        /// </summary>
        public string SuggestedSearchExpression { get; set; }

        /// <summary>
        ///     The matches found for the query
        /// </summary>
        public List<VetCompassCode> Results { get; set; }
    }
}