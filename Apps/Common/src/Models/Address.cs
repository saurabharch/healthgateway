//-------------------------------------------------------------------------
// Copyright © 2019 Province of British Columbia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-------------------------------------------------------------------------
namespace HealthGateway.Common.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an address.
    /// </summary>
    public class Address
    {
        /// <summary>
        /// Gets or sets the street lines.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Team Decision")]
        public IList<string> StreetLines { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the city name.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state/province.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Adds an address line to the internal lines list.
        /// </summary>
        /// <param name="line">The address line to add.</param>
        public void AddLine(string line)
        {
            this.StreetLines.Add(line);
        }
    }
}
