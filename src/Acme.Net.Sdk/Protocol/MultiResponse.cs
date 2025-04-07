using System.Collections.Generic;
using Newtonsoft.Json.Linq; // For JToken

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a potentially paginated response from the Accumulate API containing multiple items.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.MultiResponse.
    /// </summary>
    /// <typeparam name="T">The primary type of items expected in the response.</typeparam>
    public class MultiResponse<T>
    {
        /// <summary>
        /// Gets or sets the list of primary items in the response, successfully deserialized to type T.
        /// </summary>
        public IList<T>? Items { get; set; }

        /// <summary>
        /// Gets or sets a list of additional items that might be present in the response, stored as raw JTokens.
        /// </summary>
        public IList<JToken>? OtherItems { get; set; }

        /// <summary>
        /// Gets or sets the starting index for the items returned (for pagination).
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Gets or sets the number of items included in this response.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the total number of items available across all pages.
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiResponse{T}"/> class.
        /// </summary>
        public MultiResponse()
        {
            // Default constructor
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiResponse{T}"/> class with specified values.
        /// </summary>
        /// <param name="items">The list of primary items.</param>
        /// <param name="otherItems">The list of other items as JTokens.</param>
        /// <param name="start">The starting index.</param>
        /// <param name="count">The count of items in this response.</param>
        /// <param name="total">The total number of items available.</param>
        public MultiResponse(IList<T>? items, IList<JToken>? otherItems, long start, long count, long total)
        {
            Items = items;
            OtherItems = otherItems;
            Start = start;
            Count = count;
            Total = total;
        }
    }
}

