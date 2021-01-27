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
namespace HealthGateway.Immunization.Models
{
    using System;
    using System.Text.Json.Serialization;
    using HealthGateway.Immunization.Constants;

    /// <summary>
    /// Represents Immunization Forecast.
    /// </summary>
    public class ImmunizationForecast
    {
        /// <summary>
        /// Gets or sets the Recommendation Id.
        /// </summary>
        [JsonPropertyName("recommendationId")]
        public string RecommendationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Create Date.
        /// </summary>
        [JsonPropertyName("createDate")]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [JsonPropertyName("status")]
        public ForecastStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the Display Name.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Eligible Date.
        /// </summary>
        [JsonPropertyName("eligibleDate")]
        public DateTime EligibleDate { get; set; }

        /// <summary>
        /// Gets or sets the Due Date.
        /// </summary>
        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Creates a ImmunizationForecast object from a PHSA model.
        /// </summary>
        /// <param name="model">The immunization forecast object to convert.</param>
        /// <returns>The newly created ImmunizationForecast object.</returns>
        public static ImmunizationForecast FromPHSAModel(ImmunizationRorecastResponse model)
        {
            return new ImmunizationForecast()
            {
                RecommendationId = model.ImmsId,
                CreateDate = model.ForecastCreateDate,
                Status = (ForecastStatus)Enum.Parse(typeof(ForecastStatus), model.ForecastStatus, true),
                DisplayName = model.DisplayName,
                EligibleDate = model.EligibleDate,
                DueDate = model.DueDate
            };
        }
    }
}