// -------------------------------------------------------------------------
//  Copyright © 2019 Province of British Columbia
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// -------------------------------------------------------------------------
namespace HealthGateway.AccountDataAccess.Patient.Strategy
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentValidation;
    using HealthGateway.Common.CacheProviders;
    using HealthGateway.Common.Constants;
    using HealthGateway.Common.ErrorHandling.Exceptions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The Patient Query Strategy interface declares operations common to all supported
    /// versions of the get patient async algorithm.
    /// The Context uses this interface to call the algorithm defined by Concrete
    /// Strategies.
    /// </summary>
    internal abstract class PatientQueryStrategy
    {
        private const string PatientCacheDomain = "PatientV2";

        private readonly ILogger<PatientQueryStrategy> logger;
        private readonly ICacheProvider cacheProvider;
        private readonly int cacheTtl;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientQueryStrategy"/> class.
        /// </summary>
        /// <param name="configuration">The Configuration to use.</param>
        /// <param name="cacheProvider">The injected cache provider.</param>
        /// <param name="logger">The injected logger.</param>
        protected PatientQueryStrategy(
            IConfiguration configuration,
            ICacheProvider cacheProvider,
            ILogger<PatientQueryStrategy> logger)
        {
            this.cacheProvider = cacheProvider;
            this.logger = logger;
            this.cacheTtl = configuration.GetSection("PatientService").GetValue("CacheTTL", 0);
        }

        private static ActivitySource Source { get; } = new(nameof(PatientQueryStrategy));

        /// <summary>
        /// Returns patient model based on the implemented Strategy.
        /// </summary>
        /// <param name="request">The request parameter values to use in the query..</param>
        /// <param name="ct">The cancellation token.</param>
        /// <exception cref="NotFoundException">No patient could be found matching the provided criteria.</exception>
        /// <exception cref="ValidationException">The provided PHN identifier is invalid.</exception>
        /// <returns>A <see cref="PatientQueryStrategy"/> class.</returns>
        public abstract Task<PatientModel> GetPatientAsync(PatientRequest request, CancellationToken ct = default);

        /// <summary>
        /// Returns the logger.
        /// </summary>
        /// <returns>A <see cref="ILogger"/> class.</returns>
        protected ILogger GetLogger()
        {
            return this.logger;
        }

        /// <summary>
        /// Attempts to get the Patient model from the Generic Cache.
        /// </summary>
        /// <param name="identifier">The resource identifier used to determine the key to use.</param>
        /// <param name="identifierType">The type of patient identifier we are searching for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The found Patient model or null.</returns>
        protected async Task<PatientModel?> GetFromCacheAsync(string identifier, PatientIdentifierType identifierType, CancellationToken ct)
        {
            using Activity? activity = Source.StartActivity();
            PatientModel? retPatient = null;
            if (this.cacheTtl > 0)
            {
                switch (identifierType)
                {
                    case PatientIdentifierType.Hdid:
                        this.logger.LogDebug("Querying Patient Cache by HDID");
                        retPatient = await this.cacheProvider.GetItemAsync<PatientModel>($"{PatientCacheDomain}:HDID:{identifier}", ct);
                        break;

                    case PatientIdentifierType.Phn:
                        this.logger.LogDebug("Querying Patient Cache by PHN");
                        retPatient = await this.cacheProvider.GetItemAsync<PatientModel>($"{PatientCacheDomain}:PHN:{identifier}", ct);
                        break;
                }

                string message = $"Patient with identifier {identifier} was {(retPatient == null ? "not" : string.Empty)} found in cache";
                this.logger.LogDebug("{Message}", message);
            }

            activity?.Stop();
            return retPatient;
        }

        /// <summary>
        /// Caches the Patient model if patient is not null and validation is enabled.
        /// </summary>
        /// <param name="patient">The patient to cache.</param>
        /// <param name="disabledValidation">bool indicating if disabledValidation was set.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected async Task CachePatientAsync(PatientModel? patient, bool disabledValidation, CancellationToken ct)
        {
            // Only cache if validation is enabled (as some clients could get invalid data) and when successful.
            if (patient != null && !disabledValidation)
            {
                await this.CachePatientAsync(patient, ct);
            }
        }

        /// <summary>
        /// Caches the Patient model if enabled.
        /// </summary>
        /// <param name="patientModel">The patient to cache.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private async Task CachePatientAsync(PatientModel patientModel, CancellationToken ct)
        {
            using Activity? activity = Source.StartActivity();
            string hdid = patientModel.Hdid;
            if (this.cacheTtl > 0)
            {
                this.logger.LogDebug("Attempting to cache patient: {Hdid}", hdid);
                TimeSpan expiry = TimeSpan.FromMinutes(this.cacheTtl);
                if (!string.IsNullOrEmpty(patientModel.Hdid))
                {
                    await this.cacheProvider.AddItemAsync($"{PatientCacheDomain}:HDID:{patientModel.Hdid}", patientModel, expiry, ct);
                }

                if (!string.IsNullOrEmpty(patientModel.Phn))
                {
                    await this.cacheProvider.AddItemAsync($"{PatientCacheDomain}:PHN:{patientModel.Phn}", patientModel, expiry, ct);
                }
            }
            else
            {
                this.logger.LogDebug("Patient caching is disabled will not cache patient: {Hdid}", hdid);
            }

            activity?.Stop();
        }
    }

    /// <summary>
    /// The patient request.
    /// </summary>
    internal record PatientRequest(
        string Identifier,
        bool UseCache,
        bool DisabledValidation = false);
}
