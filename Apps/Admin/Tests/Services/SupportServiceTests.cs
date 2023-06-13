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
namespace HealthGateway.Admin.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using DeepEqual.Syntax;
    using HealthGateway.AccountDataAccess.Audit;
    using HealthGateway.AccountDataAccess.Patient;
    using HealthGateway.Admin.Common.Constants;
    using HealthGateway.Admin.Common.Models;
    using HealthGateway.Admin.Server.Services;
    using HealthGateway.Admin.Tests.Utils;
    using HealthGateway.Common.CacheProviders;
    using HealthGateway.Common.Data.Constants;
    using HealthGateway.Common.Data.ErrorHandling;
    using HealthGateway.Common.Data.Models;
    using HealthGateway.Common.Data.Utils;
    using HealthGateway.Database.Constants;
    using HealthGateway.Database.Delegates;
    using HealthGateway.Database.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    using Address = HealthGateway.AccountDataAccess.Patient.Address;
    using Name = HealthGateway.Admin.Common.Models.Name;

    /// <summary>
    /// SupportService's Unit Tests.
    /// </summary>
    public class SupportServiceTests
    {
        private const string Hdid = "DEV4FPEGCXG2NB5K2USBL52S66SC3GOUHWRP3GTXR2BTY5HEC4YA";
        private const string Hdid2 = "C3GOUHWRP3GTXR2BTY5HEC4YADEV4FPEGCXG2NB5K2USBL52S66S";
        private const string Phn = "9735361219";
        private const string Phn2 = "9219735361";
        private const string SmsNumber = "2501234567";
        private const string Email = "fakeemail@healthgateway.gov.bc.ca";

        private const string ClientRegistryWarning = "Client Registry Warning";
        private const string PatientResponseCode = $"500|{ClientRegistryWarning}";

        private const string ConfigUnixTimeZoneId = "America/Vancouver";
        private const string ConfigWindowsTimeZoneId = "Pacific Standard Time";

        private static readonly DateTime Birthdate = new(2000, 1, 1);
        private static readonly DateTime Birthdate2 = new(1999, 12, 31);

        private static readonly IMapper AutoMapper = MapperUtil.InitializeAutoMapper();
        private static readonly IConfiguration Configuration = GetIConfigurationRoot();

        /// <summary>
        /// GetMessageVerificationsAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetMessageVerificationsAsync()
        {
            // Arrange
            IList<MessagingVerification> messagingVerifications = GenerateMessagingVerifications(SmsNumber, Email);
            ISupportService supportService = CreateSupportService(GetMessagingVerificationDelegateMock(messagingVerifications));

            // Act
            PatientSupportDetails actualResult = await supportService.GetPatientSupportDetailsAsync(Hdid).ConfigureAwait(true);

            // Assert
            Assert.Equal(2, actualResult.MessagingVerifications.Count());
        }

        /// <summary>
        /// GetPatientsAsync - HDID - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByHdid()
        {
            // Arrange
            PatientDetailsQuery query = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            AccountDataAccess.Patient.Name commonName = GenerateName();
            AccountDataAccess.Patient.Name legalName = GenerateName("Jim", "Bo");
            Address physicalAddress = GenerateAddress(GenerateStreetLines());
            Address postalAddress = GenerateAddress(new List<string> { "PO BOX 1234" });
            PatientModel patient = GeneratePatientModel(Phn, Hdid, Birthdate, commonName, legalName, physicalAddress, postalAddress);
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, patient));

            UserProfile profile = new() { HdId = Hdid, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(3)), LastLoginDateTime = DateTime.Now };
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profile);

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(patient, profile),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Hdid, Hdid).ConfigureAwait(true);

            // Assert
            Assert.Single(actualResult);
            Assert.Equal(PatientStatus.Default, actualResult.First().Status);
            actualResult.ShouldDeepEqual(expectedResult);
        }

        /// <summary>
        /// GetPatientsAsync - HDID - Happy Path with Warning.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByHdidWithWarning()
        {
            // Arrange
            PatientDetailsQuery query = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            PatientModel patient = GeneratePatientModel(Phn, Hdid, Birthdate, responseCode: PatientResponseCode);
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, patient));

            UserProfile profile = new() { HdId = Hdid, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(3)), LastLoginDateTime = DateTime.Now };
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profile);

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Hdid, Hdid).ConfigureAwait(true);

            // Assert
            Assert.Single(actualResult);
            Assert.Equal(PatientStatus.Default, actualResult.First().Status);
            Assert.Equal(ClientRegistryWarning, actualResult.First().WarningMessage);
        }

        /// <summary>
        /// GetPatientsAsync - HDID - Not Found.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByHdidNotFound()
        {
            // Arrange
            PatientDetailsQuery query = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            PatientModel? patient = null;
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, patient));

            UserProfile profile = new() { HdId = Hdid, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(3)), LastLoginDateTime = DateTime.Now };
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profile);

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(patient, profile),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Hdid, Hdid).ConfigureAwait(true);

            // Assert
            Assert.Single(actualResult);
            Assert.Equal(PatientStatus.NotFound, actualResult.First().Status);
            actualResult.ShouldDeepEqual(expectedResult);
        }

        /// <summary>
        /// GetPatientsAsync - HDID - Deceased.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByHdidDeceased()
        {
            // Arrange
            PatientDetailsQuery query = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            AccountDataAccess.Patient.Name commonName = GenerateName();
            AccountDataAccess.Patient.Name legalName = GenerateName("Jim", "Bo");
            Address physicalAddress = GenerateAddress(GenerateStreetLines());
            Address postalAddress = GenerateAddress(new List<string> { "PO BOX 1234" });
            PatientModel patient = GeneratePatientModel(Phn, Hdid, Birthdate, commonName, legalName, physicalAddress, postalAddress, isDeceased: true);
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, patient));

            UserProfile profile = new() { HdId = Hdid, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(3)), LastLoginDateTime = DateTime.Now };
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profile);

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(patient, profile),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Hdid, Hdid).ConfigureAwait(true);

            // Assert
            Assert.Single(actualResult);
            Assert.Equal(PatientStatus.Deceased, actualResult.First().Status);
            actualResult.ShouldDeepEqual(expectedResult);
        }

        /// <summary>
        /// GetPatientsAsync - HDID - Not User.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByHdidNotUser()
        {
            // Arrange
            PatientDetailsQuery query = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            AccountDataAccess.Patient.Name commonName = GenerateName();
            AccountDataAccess.Patient.Name legalName = GenerateName("Jim", "Bo");
            Address physicalAddress = GenerateAddress(GenerateStreetLines());
            Address postalAddress = GenerateAddress(new List<string> { "PO BOX 1234" });
            PatientModel patient = GeneratePatientModel(Phn, Hdid, Birthdate, commonName, legalName, physicalAddress, postalAddress);
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, patient));

            UserProfile? profile = null;
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profile);

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(patient, profile),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Hdid, Hdid).ConfigureAwait(true);

            // Assert
            Assert.Single(actualResult);
            Assert.Equal(PatientStatus.NotUser, actualResult.First().Status);
            actualResult.ShouldDeepEqual(expectedResult);
        }

        /// <summary>
        /// GetPatientsAsync - HDID - Not Found and Not User.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByHdidNotFoundNotUser()
        {
            // Arrange
            PatientDetailsQuery query = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, null));
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock();

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Hdid, Hdid).ConfigureAwait(true);

            // Assert
            Assert.Empty(actualResult);
        }

        /// <summary>
        /// GetPatientsAsync - PHN - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByPhn()
        {
            // Arrange
            PatientDetailsQuery query = new() { Phn = Phn, Source = PatientDetailSource.Empi, UseCache = false };
            AccountDataAccess.Patient.Name commonName = GenerateName();
            AccountDataAccess.Patient.Name legalName = GenerateName("Jim", "Bo");
            Address physicalAddress = GenerateAddress(GenerateStreetLines());
            Address postalAddress = GenerateAddress(new List<string> { "PO BOX 1234" });
            PatientModel patient = GeneratePatientModel(Phn, Hdid, Birthdate, commonName, legalName, physicalAddress, postalAddress);
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, patient));

            UserProfile profile = new() { HdId = Hdid, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(3)), LastLoginDateTime = DateTime.Now };
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profile);

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(patient, profile),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Phn, Phn).ConfigureAwait(true);

            // Assert
            Assert.Single(actualResult);
            Assert.Equal(PatientStatus.Default, actualResult.First().Status);
            actualResult.ShouldDeepEqual(expectedResult);
        }

        /// <summary>
        /// GetPatientsAsync - PHN - Not Found and Not User.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientByPhnNotFoundNotUser()
        {
            // Arrange
            PatientDetailsQuery query = new() { Phn = Phn, Source = PatientDetailSource.Empi, UseCache = false };
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((query, null));
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock();

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Phn, Phn).ConfigureAwait(true);

            // Assert
            Assert.Empty(actualResult);
        }

        /// <summary>
        /// GetPatientsAsync - Invalid Arguments Result in Exception.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetPatientShouldThrowBadRequest()
        {
            // Arrange
            ISupportService supportService = CreateSupportService();

            // Act
            ProblemDetailsException exception = await Assert.ThrowsAsync<ProblemDetailsException>(async () => await supportService.GetPatientsAsync((PatientQueryType)99, Hdid).ConfigureAwait(true))
                .ConfigureAwait(true);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, exception.ProblemDetails?.StatusCode);
        }

        /// <summary>
        /// GetPatientsAsync - Dependent - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientsByDependentHdid()
        {
            // Arrange
            const string dependentPhn = "dependentPhn";
            const string dependentHdid = "dependentHdid";
            PatientDetailsQuery dependentQuery = new() { Phn = dependentPhn, Source = PatientDetailSource.Empi, UseCache = false };
            PatientModel dependentPatient = GeneratePatientModel(dependentPhn, dependentHdid, Birthdate);

            PatientDetailsQuery firstDelegateQuery = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            AccountDataAccess.Patient.Name commonName = GenerateName();
            AccountDataAccess.Patient.Name legalName = GenerateName("Jim", "Bo");
            Address physicalAddress = GenerateAddress(GenerateStreetLines());
            Address postalAddress = GenerateAddress(new List<string> { "PO BOX 1234" });
            PatientModel firstDelegatePatient = GeneratePatientModel(Phn, Hdid, Birthdate, commonName, legalName, physicalAddress, postalAddress);

            PatientDetailsQuery secondDelegateQuery = new() { Hdid = Hdid2, Source = PatientDetailSource.All, UseCache = false };
            PatientModel secondDelegatePatient = GeneratePatientModel(Phn2, Hdid2, Birthdate2);

            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock(
                (dependentQuery, dependentPatient),
                (firstDelegateQuery, firstDelegatePatient),
                (secondDelegateQuery, secondDelegatePatient));

            ResourceDelegateQuery resourceDelegateQuery = new() { ByOwnerHdid = dependentHdid, IncludeProfile = true, TakeAmount = 25 };
            IList<ResourceDelegate> resourceDelegates = GenerateResourceDelegates(dependentHdid, new List<string> { Hdid, Hdid2 }, DateTime.Now.Subtract(TimeSpan.FromDays(1)), DateTime.Now);
            Mock<IResourceDelegateDelegate> resourceDelegateDelegateMock = GetResourceDelegateDelegateMock(resourceDelegateQuery, resourceDelegates);

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, resourceDelegateDelegateMock: resourceDelegateDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(firstDelegatePatient, resourceDelegates.ElementAt(0).UserProfile),
                GetExpectedPatientSupportDetails(secondDelegatePatient, resourceDelegates.ElementAt(1).UserProfile),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Dependent, dependentPhn).ConfigureAwait(true);

            // Assert
            Assert.Equal(resourceDelegates.Count, actualResult.Count());
            actualResult.ShouldDeepEqual(expectedResult);
        }

        /// <summary>
        /// GetPatientsAsync - Email - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientsByEmail()
        {
            // Arrange
            List<UserProfile> profiles = new()
            {
                new() { HdId = Hdid, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(3)), LastLoginDateTime = DateTime.Now },
                new() { HdId = Hdid2, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(5)), LastLoginDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(1)) },
            };
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profiles: profiles);

            PatientDetailsQuery firstQuery = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            AccountDataAccess.Patient.Name commonName = GenerateName();
            AccountDataAccess.Patient.Name legalName = GenerateName("Jim", "Bo");
            Address physicalAddress = GenerateAddress(GenerateStreetLines());
            Address postalAddress = GenerateAddress(new List<string> { "PO BOX 1234" });
            PatientModel firstPatient = GeneratePatientModel(Phn, Hdid, Birthdate, commonName, legalName, physicalAddress, postalAddress);

            PatientDetailsQuery secondQuery = new() { Hdid = Hdid2, Source = PatientDetailSource.All, UseCache = false };
            PatientModel secondPatient = GeneratePatientModel(Phn2, Hdid2, Birthdate2);
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((firstQuery, firstPatient), (secondQuery, secondPatient));

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(firstPatient, profiles[0]),
                GetExpectedPatientSupportDetails(secondPatient, profiles[1]),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Email, "email").ConfigureAwait(true);

            // Assert
            Assert.Equal(profiles.Count, actualResult.Count());
            actualResult.ShouldDeepEqual(expectedResult);
        }

        /// <summary>
        /// GetPatientsAsync - SMS - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetPatientsBySms()
        {
            // Arrange
            List<UserProfile> profiles = new()
            {
                new() { HdId = Hdid, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(3)), LastLoginDateTime = DateTime.Now },
                new() { HdId = Hdid2, CreatedDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(5)), LastLoginDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(1)) },
            };
            Mock<IUserProfileDelegate> userProfileDelegateMock = GetUserProfileDelegateMock(profiles: profiles);

            PatientDetailsQuery firstQuery = new() { Hdid = Hdid, Source = PatientDetailSource.All, UseCache = false };
            AccountDataAccess.Patient.Name commonName = GenerateName();
            AccountDataAccess.Patient.Name legalName = GenerateName("Jim", "Bo");
            Address physicalAddress = GenerateAddress(GenerateStreetLines());
            Address postalAddress = GenerateAddress(new List<string> { "PO BOX 1234" });
            PatientModel firstPatient = GeneratePatientModel(Phn, Hdid, Birthdate, commonName, legalName, physicalAddress, postalAddress);

            PatientDetailsQuery secondQuery = new() { Hdid = Hdid2, Source = PatientDetailSource.All, UseCache = false };
            PatientModel secondPatient = GeneratePatientModel(Phn2, Hdid2, Birthdate2);
            Mock<IPatientRepository> patientRepositoryMock = GetPatientRepositoryMock((firstQuery, firstPatient), (secondQuery, secondPatient));

            ISupportService supportService = CreateSupportService(patientRepositoryMock: patientRepositoryMock, userProfileDelegateMock: userProfileDelegateMock);

            IEnumerable<PatientSupportResult> expectedResult = new List<PatientSupportResult>
            {
                GetExpectedPatientSupportDetails(firstPatient, profiles[0]),
                GetExpectedPatientSupportDetails(secondPatient, profiles[1]),
            };

            // Act
            IEnumerable<PatientSupportResult> actualResult = await supportService.GetPatientsAsync(PatientQueryType.Sms, "sms").ConfigureAwait(true);

            // Assert
            Assert.Equal(profiles.Count, actualResult.Count());
            actualResult.ShouldDeepEqual(expectedResult);
        }

        private static PatientSupportResult GetExpectedPatientSupportDetails(PatientModel? patient, UserProfile? profile)
        {
            PatientStatus status = PatientStatus.Default;
            if (patient == null)
            {
                status = PatientStatus.NotFound;
            }
            else if (patient.IsDeceased == true)
            {
                status = PatientStatus.Deceased;
            }
            else if (profile == null)
            {
                status = PatientStatus.NotUser;
            }

            return new()
            {
                Status = status,
                WarningMessage = string.Empty,
                Hdid = patient?.Hdid ?? profile?.HdId ?? string.Empty,
                PersonalHealthNumber = patient?.Phn ?? string.Empty,
                CommonName = Map(patient?.CommonName),
                LegalName = Map(patient?.LegalName),
                Birthdate = patient == null ? null : DateOnly.FromDateTime(patient.Birthdate),
                PhysicalAddress = AddressUtility.GetAddressAsSingleLine(AutoMapper.Map<HealthGateway.Common.Data.Models.Address?>(patient?.PhysicalAddress)),
                PostalAddress = AddressUtility.GetAddressAsSingleLine(AutoMapper.Map<HealthGateway.Common.Data.Models.Address?>(patient?.PostalAddress)),
                ProfileCreatedDateTime = profile?.CreatedDateTime,
                ProfileLastLoginDateTime = profile?.LastLoginDateTime,
            };
        }

        private static Name? Map(AccountDataAccess.Patient.Name? name)
        {
            return name == null ? null : new Name { GivenName = name.GivenName, Surname = name.Surname };
        }

        private static IList<MessagingVerification> GenerateMessagingVerifications(string sms, string email)
        {
            return new List<MessagingVerification>
            {
                new() { Id = Guid.NewGuid(), Validated = true, SmsNumber = sms },
                new() { Id = Guid.NewGuid(), Validated = false, Email = new() { To = email } },
            };
        }

        private static IList<ResourceDelegate> GenerateResourceDelegates(
            string dependentHdid,
            IEnumerable<string> delegateHdids,
            DateTime profileCreatedDateTime,
            DateTime profileLastLoginDateTime)
        {
            return delegateHdids.Select(
                    delegateHdid => new ResourceDelegate
                    {
                        ResourceOwnerHdid = dependentHdid,
                        ProfileHdid = delegateHdid,
                        UserProfile = new() { HdId = delegateHdid, CreatedDateTime = profileCreatedDateTime, LastLoginDateTime = profileLastLoginDateTime },
                    })
                .ToList();
        }

        private static AccountDataAccess.Patient.Name GenerateName(string givenName = "John", string surname = "Doe")
        {
            return new AccountDataAccess.Patient.Name { GivenName = givenName, Surname = surname };
        }

        private static IEnumerable<string> GenerateStreetLines()
        {
            return new List<string> { "Line 1", "Line 2", "Physical" };
        }

        private static Address GenerateAddress(IEnumerable<string> streetLines, string city = "City", string state = "BC", string postalCode = "N0N0N0")
        {
            return new()
            {
                StreetLines = streetLines,
                City = city,
                State = state,
                Country = "CA",
                PostalCode = postalCode,
            };
        }

        private static PatientModel GeneratePatientModel(
            string phn,
            string hdid,
            DateTime birthdate,
            AccountDataAccess.Patient.Name? commonName = null,
            AccountDataAccess.Patient.Name? legalName = null,
            Address? physicalAddress = null,
            Address? postalAddress = null,
            string? responseCode = null,
            bool isDeceased = false)
        {
            return new()
            {
                Phn = phn,
                Hdid = hdid,
                Birthdate = birthdate,
                CommonName = commonName,
                LegalName = legalName,
                PhysicalAddress = physicalAddress,
                PostalAddress = postalAddress,
                ResponseCode = responseCode ?? string.Empty,
                IsDeceased = isDeceased,
            };
        }

        private static Mock<IMessagingVerificationDelegate> GetMessagingVerificationDelegateMock(IList<MessagingVerification> result)
        {
            Mock<IMessagingVerificationDelegate> mock = new();
            mock.Setup(d => d.GetUserMessageVerificationsAsync(It.IsAny<string>())).ReturnsAsync(result);
            return mock;
        }

        private static Mock<IPatientRepository> GetPatientRepositoryMock(params (PatientDetailsQuery Query, PatientModel? Patient)[] pairs)
        {
            Mock<IPatientRepository> mock = new();
            foreach ((PatientDetailsQuery query, PatientModel? patient) in pairs)
            {
                PatientQueryResult result = new(patient == null ? Enumerable.Empty<PatientModel>() : new List<PatientModel> { patient });
                mock.Setup(p => p.Query(query, It.IsAny<CancellationToken>())).ReturnsAsync(result);
            }

            return mock;
        }

        private static Mock<IResourceDelegateDelegate> GetResourceDelegateDelegateMock(ResourceDelegateQuery query, IEnumerable<ResourceDelegate> result)
        {
            Mock<IResourceDelegateDelegate> mock = new();
            mock.Setup(d => d.SearchAsync(query)).ReturnsAsync(new ResourceDelegateQueryResult { Items = result });
            return mock;
        }

        private static Mock<IUserProfileDelegate> GetUserProfileDelegateMock(UserProfile? profile = null, IList<UserProfile>? profiles = null)
        {
            Mock<IUserProfileDelegate> mock = new();
            mock.Setup(u => u.GetUserProfileAsync(It.IsAny<string>())).ReturnsAsync(profile);
            mock.Setup(u => u.GetUserProfilesAsync(It.IsAny<UserQueryType>(), It.IsAny<string>())).ReturnsAsync(profiles ?? Array.Empty<UserProfile>());
            return mock;
        }

        private static ISupportService CreateSupportService(
            Mock<IMessagingVerificationDelegate>? messagingVerificationDelegateMock = null,
            Mock<IPatientRepository>? patientRepositoryMock = null,
            Mock<IResourceDelegateDelegate>? resourceDelegateDelegateMock = null,
            Mock<IUserProfileDelegate>? userProfileDelegateMock = null)
        {
            userProfileDelegateMock ??= new Mock<IUserProfileDelegate>();
            messagingVerificationDelegateMock ??= new Mock<IMessagingVerificationDelegate>();
            patientRepositoryMock ??= new Mock<IPatientRepository>();
            resourceDelegateDelegateMock ??= new Mock<IResourceDelegateDelegate>();

            return new SupportService(
                AutoMapper,
                Configuration,
                messagingVerificationDelegateMock.Object,
                patientRepositoryMock.Object,
                resourceDelegateDelegateMock.Object,
                userProfileDelegateMock.Object,
                new Mock<IAuditRepository>().Object,
                new Mock<ICacheProvider>().Object,
                new Mock<ILogger<SupportService>>().Object);
        }

        private static IConfigurationRoot GetIConfigurationRoot()
        {
            Dictionary<string, string?> myConfiguration = new()
            {
                { "TimeZone:UnixTimeZoneId", ConfigUnixTimeZoneId },
                { "TimeZone:WindowsTimeZoneId", ConfigWindowsTimeZoneId },
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration.ToList())
                .Build();
        }
    }
}