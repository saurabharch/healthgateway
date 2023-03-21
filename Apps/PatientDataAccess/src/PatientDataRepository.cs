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
namespace HealthGateway.PatientDataAccess
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using HealthGateway.PatientDataAccess.Api;
    using Refit;

    /// <summary>
    /// Provides internal data access for Patient.
    /// </summary>
    internal class PatientDataRepository : IPatientDataRepository
    {
        private readonly IPatientApi patientApi;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientDataRepository"/> class.
        /// </summary>
        /// <param name="patientApi">The patient API to use.</param>
        /// <param name="mapper">The injected mapper.</param>
        public PatientDataRepository(IPatientApi patientApi, IMapper mapper)
        {
            this.patientApi = patientApi;
            this.mapper = mapper;
        }

        /// <summary>
        /// Performs a query against the data respository.
        /// </summary>
        /// <param name="query">The query to perform.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The query result.</returns>
        /// <exception cref="NotImplementedException">Thrown if query is not implemented.</exception>
        public async Task<PatientDataQueryResult> Query(PatientDataQuery query, CancellationToken ct)
        {
            return query switch
            {
                HealthServicesQuery q => await this.Handle(q, ct).ConfigureAwait(true),
                PatientFileQuery q => await this.Handle(q, ct).ConfigureAwait(true),
                _ => throw new NotImplementedException($"{query.GetType().Name} doesn't have a handler"),
            };
        }

        private static string Map(HealthServiceCategory category) =>
            category switch
            {
                HealthServiceCategory.OrganDonor => "BcTransplantOrganDonor",
                _ => throw new NotImplementedException($"No mapping implemented for {category}"),
            };

        private static PatientFile Map(string fileId, FileResult file) =>
            new(fileId, Encoding.Default.GetBytes(file.Data!), file.MediaType ?? string.Empty);

        private async Task<PatientDataQueryResult> Handle(HealthServicesQuery query, CancellationToken ct)
        {
            string[] categories = query.Categories.Select(c => Map(c)).ToArray();

            HealthOptionsResult results = await this.patientApi.GetHealthOptionsAsync(query.Pid, categories, ct).ConfigureAwait(true) ?? new(new HealthOptionMetadata(), Array.Empty<HealthOptionData>());
            return new PatientDataQueryResult(results.Data.Select(this.Map));
        }

        private async Task<PatientDataQueryResult> Handle(PatientFileQuery query, CancellationToken ct)
        {
            try
            {
                var fileResult = await this.patientApi.GetFile(query.Pid, query.FileId, ct).ConfigureAwait(true);
                var mappedFiles = new[] { fileResult }
                    .Where(f => f?.Data != null)
                    .Select(f => Map(query.FileId, f!));
                return new PatientDataQueryResult(mappedFiles);
            }
            catch (ApiException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // file not found
                return new PatientDataQueryResult(Array.Empty<HealthData>());
            }
        }

        private HealthData Map(HealthOptionData healthOptionData) => this.mapper.Map<HealthData>(healthOptionData);
    }
}