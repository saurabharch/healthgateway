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
namespace HealthGateway.GatewayApi.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using HealthGateway.Common.Models.PHSA;
    using HealthGateway.Common.Services;
    using HealthGateway.GatewayApi.Api;
    using HealthGateway.GatewayApi.Models;
    using HealthGateway.GatewayApi.Models.Phsa;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service to interact with the the PHSA web alert API.
    /// </summary>
    public class WebAlertService : IWebAlertService
    {
        private readonly ILogger<WebAlertService> logger;
        private readonly IPersonalAccountsService personalAccountsService;
        private readonly IWebAlertApi webAlertApi;
        private readonly IMapper autoMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebAlertService"/> class.
        /// </summary>
        /// <param name="logger">The injected logger.</param>
        /// <param name="personalAccountsService">The injected personal accounts service.</param>
        /// <param name="webAlertApi">The injected Refit API.</param>
        /// <param name="autoMapper">The injected automapper.</param>
        public WebAlertService(ILogger<WebAlertService> logger, IPersonalAccountsService personalAccountsService, IWebAlertApi webAlertApi, IMapper autoMapper)
        {
            this.logger = logger;
            this.personalAccountsService = personalAccountsService;
            this.webAlertApi = webAlertApi;
            this.autoMapper = autoMapper;
        }

        private static ActivitySource Source { get; } = new(nameof(WebAlertService));

        /// <inheritdoc/>
        public async Task<IList<WebAlert>> GetWebAlertsAsync(string hdid)
        {
            using Activity? activity = Source.StartActivity();
            this.logger.LogDebug("Retrieving web alerts from PHSA.");
            PersonalAccount personalAccount = await this.personalAccountsService.GetPatientAccountAsync(hdid).ConfigureAwait(true);
            string accountId = personalAccount.Id.ToString();

            IList<PhsaWebAlert> phsaWebAlerts = await this.webAlertApi.GetWebAlertsAsync(accountId).ConfigureAwait(true);
            IList<WebAlert> webAlerts = this.autoMapper.Map<IEnumerable<PhsaWebAlert>, IList<WebAlert>>(
                phsaWebAlerts.Where(a => a.ExpirationDateTimeUtc > DateTime.UtcNow && a.ScheduledDateTimeUtc < DateTime.UtcNow)
                    .OrderByDescending(a => a.ScheduledDateTimeUtc));
            this.logger.LogDebug("Finished retrieving web alerts from PHSA.");
            return webAlerts;
        }

        /// <inheritdoc/>
        public async Task DismissWebAlertsAsync(string hdid)
        {
            using Activity? activity = Source.StartActivity();
            this.logger.LogDebug("Sending request to dismiss web alerts to PHSA.");
            PersonalAccount personalAccount = await this.personalAccountsService.GetPatientAccountAsync(hdid).ConfigureAwait(true);
            string pid = personalAccount.PatientIdentity.Pid.ToString();

            await this.webAlertApi.DeleteWebAlertsAsync(pid).ConfigureAwait(true);
            this.logger.LogDebug("Finished sending request to dismiss web alerts to PHSA.");
        }

        /// <inheritdoc/>
        public async Task DismissWebAlertAsync(string hdid, Guid webAlertId)
        {
            using Activity? activity = Source.StartActivity();
            this.logger.LogDebug("Sending request to dismiss web alert to PHSA.");
            PersonalAccount personalAccount = await this.personalAccountsService.GetPatientAccountAsync(hdid).ConfigureAwait(true);
            string pid = personalAccount.PatientIdentity.Pid.ToString();

            await this.webAlertApi.DeleteWebAlertAsync(pid, webAlertId).ConfigureAwait(true);
            this.logger.LogDebug("Finished sending request to dismiss web alert to PHSA.");
        }
    }
}
