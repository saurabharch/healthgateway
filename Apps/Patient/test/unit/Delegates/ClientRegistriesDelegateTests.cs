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
namespace HealthGateway.PatientTests.Delegates
{
    using System;
    using System.Globalization;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using DeepEqual.Syntax;
    using HealthGateway.Common.Constants;
    using HealthGateway.Common.Data.ErrorHandling;
    using HealthGateway.Common.Data.ViewModels;
    using HealthGateway.Common.Models;
    using HealthGateway.Patient.Delegates;
    using Microsoft.Extensions.Logging;
    using Moq;
    using ServiceReference;
    using Xunit;

    /// <summary>
    /// ClientRegistriesDelegate's Unit Tests.
    /// </summary>
    public class ClientRegistriesDelegateTests
    {
        private const string Hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
        private const string Phn = "0009735353315";
        private const string FirstName = "John";
        private const string LastName = "Doe";
        private const string ResponseCode = "BCHCIM.GD.0.0019";

        /// <summary>
        /// Lookup by PHN but no HDID found.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task LookUpByPhnNoHdid()
        {
            // Setup
            string expectedResponseCode = "BCHCIM.GD.0.0013";
            string expectedPhn = "0009735353315";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";
            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, "9875023209").ConfigureAwait(true);

            // Verify
            Assert.NotEmpty(actual.Warning?.Message);
        }

        /// <summary>
        /// GetDemographicsByHDID - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldGetDemographics()
        {
            // Setup
            string expectedHdId = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0013";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";
            string expectedGender = "Female";
            Address expectedPhysicalAddr = new()
            {
                StreetLines = { "Line 1", "Line 2", "Physical" },
                City = "city",
                Country = "CA",
                PostalCode = "N0N0N0",
                State = "BC",
            };
            Address expectedPostalAddr = new()
            {
                StreetLines = { "Line 1", "Line 2", "Postal" },
                City = "city",
                Country = "CA",
                PostalCode = "N0N0N0",
                State = "BC",
            };
            DateTime expectedBirthDate = DateTime.ParseExact("20001231", "yyyyMMdd", CultureInfo.InvariantCulture);

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = expectedHdId,
                        displayable = true,
                    },
                },
                addr = new AD[]
                {
                    new()
                    {
                        use = new[]
                        {
                            cs_PostalAddressUse.PHYS,
                        },
                        Items = new ADXP[]
                        {
                            new ADStreetAddressLine
                            {
                                Text = new[]
                                {
                                    expectedPhysicalAddr.StreetLines[0],
                                    expectedPhysicalAddr.StreetLines[1],
                                    expectedPhysicalAddr.StreetLines[2],
                                },
                            },
                            new ADCity
                            {
                                Text = new[]
                                {
                                    expectedPhysicalAddr.City,
                                },
                            },
                            new ADState
                            {
                                Text = new[]
                                {
                                    expectedPhysicalAddr.State,
                                },
                            },
                            new ADPostalCode
                            {
                                Text = new[]
                                {
                                    expectedPhysicalAddr.PostalCode,
                                },
                            },
                            new ADCountry
                            {
                                Text = new[]
                                {
                                    expectedPhysicalAddr.Country,
                                },
                            },
                        },
                    },
                    new()
                    {
                        use = new[]
                        {
                            cs_PostalAddressUse.PST,
                        },
                        Items = new ADXP[]
                        {
                            new ADStreetAddressLine
                            {
                                Text = new[]
                                {
                                    expectedPostalAddr.StreetLines[0],
                                    expectedPostalAddr.StreetLines[1],
                                    expectedPostalAddr.StreetLines[2],
                                },
                            },
                            new ADCity
                            {
                                Text = new[]
                                {
                                    expectedPostalAddr.City,
                                },
                            },
                            new ADState
                            {
                                Text = new[]
                                {
                                    expectedPostalAddr.State,
                                },
                            },
                            new ADPostalCode
                            {
                                Text = new[]
                                {
                                    expectedPostalAddr.PostalCode,
                                },
                            },
                            new ADCountry
                            {
                                Text = new[]
                                {
                                    expectedPostalAddr.Country,
                                },
                            },
                        },
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Hdid, expectedHdId).ConfigureAwait(true);

            // Verify
            Assert.Equal(expectedHdId, actual.ResourcePayload?.HdId);
            Assert.Equal(expectedPhn, actual.ResourcePayload?.PersonalHealthNumber);
            Assert.Equal(expectedFirstName, actual.ResourcePayload?.FirstName);
            Assert.Equal(expectedLastName, actual.ResourcePayload?.LastName);
            Assert.Equal(expectedBirthDate, actual.ResourcePayload?.Birthdate);
            Assert.Equal(expectedGender, actual.ResourcePayload?.Gender);
            expectedPhysicalAddr.ShouldDeepEqual(actual.ResourcePayload?.PhysicalAddress);
            expectedPostalAddr.ShouldDeepEqual(actual.ResourcePayload?.PostalAddress);
        }

        /// <summary>
        /// GetDemographics - Invalid ID Error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldErrorIfInvalidIdentifier()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0013";
            string expectedFirstName = "John";
            string expectedLastName = "Doe";

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = hdid,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "01010101010",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Hdid, hdid).ConfigureAwait(true);

            // Verify
            Assert.Equal(ActionType.NoHdId.Value, actual.Warning?.Code);
            Assert.Equal(ErrorMessages.InvalidServicesCard, actual.Warning?.Message);
        }

        /// <summary>
        /// GetDemographicsByHDID - Happy Path (Validate Multiple Names).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldUseCorrectNameSection()
        {
            // Setup
            string expectedHdId = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0013";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";
            string expectedGender = "Female";
            DateTime expectedBirthDate = DateTime.ParseExact("20001231", "yyyyMMdd", CultureInfo.InvariantCulture);

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = expectedHdId,
                        displayable = true,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { "Wrong Given Name" },
                                },
                                new enfamily
                                {
                                    Text = new[] { "Wrong Family Name" },
                                },
                            },
                            use = new[] { cs_EntityNameUse.L },
                        },
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Hdid, expectedHdId).ConfigureAwait(true);

            // Verify
            Assert.Equal(expectedHdId, actual.ResourcePayload?.HdId);
            Assert.Equal(expectedPhn, actual.ResourcePayload?.PersonalHealthNumber);
            Assert.Equal(expectedFirstName, actual.ResourcePayload?.FirstName);
            Assert.Equal(expectedLastName, actual.ResourcePayload?.LastName);
            Assert.Equal(expectedBirthDate, actual.ResourcePayload?.Birthdate);
            Assert.Equal(expectedGender, actual.ResourcePayload?.Gender);
        }

        /// <summary>
        /// GetDemographicsByHDID - Happy Path (Validate Multiple Names Qualifiers).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldUseCorrectNameQualifier()
        {
            // Setup
            string expectedHdId = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0013";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";
            string expectedGender = "Female";
            DateTime expectedBirthDate = DateTime.ParseExact("20001231", "yyyyMMdd", CultureInfo.InvariantCulture);

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = expectedHdId,
                        displayable = true,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { "Wrong Given Name" },
                                },
                                new enfamily
                                {
                                    Text = new[] { "Wrong Family Name" },
                                },
                            },
                            use = new[] { cs_EntityNameUse.L },
                        },
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                    qualifier = new[]
                                    {
                                        cs_EntityNamePartQualifier.AC,
                                    },
                                },
                                new engiven
                                {
                                    qualifier = new[]
                                    {
                                        cs_EntityNamePartQualifier.CL,
                                    },
                                    Text = new[] { "Bad First Name" },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                    qualifier = new[]
                                    {
                                        cs_EntityNamePartQualifier.IN,
                                    },
                                },
                                new enfamily
                                {
                                    qualifier = new[]
                                    {
                                        cs_EntityNamePartQualifier.CL,
                                    },
                                    Text = new[] { "Bad Last Name" },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Hdid, expectedHdId).ConfigureAwait(true);

            // Verify
            Assert.Equal(expectedHdId, actual.ResourcePayload?.HdId);
            Assert.Equal(expectedPhn, actual.ResourcePayload?.PersonalHealthNumber);
            Assert.Equal(expectedFirstName, actual.ResourcePayload?.FirstName);
            Assert.Equal(expectedLastName, actual.ResourcePayload?.LastName);
            Assert.Equal(expectedBirthDate, actual.ResourcePayload?.Birthdate);
            Assert.Equal(expectedGender, actual.ResourcePayload?.Gender);
        }

        /// <summary>
        /// Get patient by PHN is subject of overlay returns success.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturnSuccessForSubjectOfOverlay()
        {
            // Setup
            string expectedHdId = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0019";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = expectedHdId,
                        displayable = true,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, expectedPhn).ConfigureAwait(true);

            // Verify
            Assert.Contains("BCHCIM.GD.0.0019", actual.ResourcePayload?.ResponseCode, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Get patient by PHN is subject of potential duplicate returns success.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturnSuccessForSubjectOfPotentialDuplicate()
        {
            // Setup
            string expectedHdId = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0021";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = expectedHdId,
                        displayable = true,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, expectedPhn).ConfigureAwait(true);

            // Verify
            Assert.Contains("BCHCIM.GD.0.0021", actual.ResourcePayload?.ResponseCode, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Get patient by PHN is subject of potential linkage returns success.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturnSuccessForSubjectOfPotentialLinkage()
        {
            // Setup
            string expectedHdId = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0022";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = expectedHdId,
                        displayable = true,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, expectedPhn).ConfigureAwait(true);

            // Verify
            Assert.Contains("BCHCIM.GD.0.0022", actual.ResourcePayload?.ResponseCode, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Get patient by PHN is subject of review identifier returns success.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturnSuccessForSubjectOfReviewIdentifier()
        {
            // Setup
            string expectedHdId = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string expectedPhn = "0009735353315";
            string expectedResponseCode = "BCHCIM.GD.0.0023";
            string expectedFirstName = "Jane";
            string expectedLastName = "Doe";

            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = new()
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = expectedHdId,
                        displayable = true,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    id = new[]
                    {
                        new II
                        {
                            root = "2.16.840.1.113883.3.51.1.1.6.1",
                            extension = expectedPhn,
                        },
                    },
                    name = new[]
                    {
                        new PN
                        {
                            Items = new ENXP[]
                            {
                                new engiven
                                {
                                    Text = new[] { expectedFirstName },
                                },
                                new enfamily
                                {
                                    Text = new[] { expectedLastName },
                                },
                            },
                            use = new[] { cs_EntityNameUse.C },
                        },
                    },
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };

            Mock<QUPA_AR101102_PortType> clientMock = new();
            clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                .ReturnsAsync(
                    new HCIM_IN_GetDemographicsResponse1
                    {
                        HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                        {
                            controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                            {
                                queryAck = new HCIM_MT_QueryResponseQueryAck
                                {
                                    queryResponseCode = new CS
                                    {
                                        code = expectedResponseCode,
                                    },
                                },
                                subject = new[]
                                {
                                    new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                                    {
                                        target = subjectTarget,
                                    },
                                },
                            },
                        },
                    });
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            IClientRegistriesDelegate patientDelegate = new ClientRegistriesDelegate(
                loggerFactory.CreateLogger<ClientRegistriesDelegate>(),
                clientMock.Object);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, expectedPhn).ConfigureAwait(true);

            // Verify
            Assert.Contains("BCHCIM.GD.0.0023", actual.ResourcePayload?.ResponseCode, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Client registry get demographics returns warning given client registry could not find legal name.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturnWarningGivenNoLegalName()
        {
            // Setup
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(false, true);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn).ConfigureAwait(true);

            // Verify
            Assert.Equal(ActionType.InvalidName.Value, actual.Warning?.Code);
            Assert.Equal(ErrorMessages.InvalidServicesCard, actual.Warning?.Message);
        }

        /// <summary>
        /// Client registry get demographics returns warning given client registry could not find any ids.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturnWarningGivenNoIds()
        {
            // Setup
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(false, false, true);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn).ConfigureAwait(true);

            // Verify
            Assert.Equal(ActionType.NoHdId.Value, actual.Warning?.Code);
            Assert.Equal(ErrorMessages.InvalidServicesCard, actual.Warning?.Message);
        }

        /// <summary>
        /// Client registry get demographics returns warning given user deceased indicator is true.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturnWarningGivenDeceasedIndicatorTrue()
        {
            // Setup
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(true);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn).ConfigureAwait(true);

            // Verify
            Assert.Equal(ActionType.Deceased.Value, actual.Warning?.Code);
            Assert.Equal(ErrorMessages.ClientRegistryReturnedDeceasedPerson, actual.Warning?.Message);
        }

        /// <summary>
        /// Client registry get demographics does not return warning for action type NoHdId given disabled id validation is set to
        /// false.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldNotReturnWarningGivenDisabledIdValidationIsFalse()
        {
            // Setup
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(false, false, true);

            // Act
            ApiResult<PatientModel> actual = await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn, true).ConfigureAwait(true);

            // Verify
            Assert.Null(actual.Warning);
        }

        /// <summary>
        /// Client registry get demographics throws ApiException given CommunicationException.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldThrowApiPatientExceptionGivenCommunicationException()
        {
            // Setup
            string expectedDetail = $"Communication Exception with client registry when trying to retrieve patient information from {OidType.Phn}";
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(false, false, false, true);

            // Act
            async Task Actual()
            {
                await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn).ConfigureAwait(true);
            }

            // Verify
            ApiException exception = await Assert.ThrowsAsync<ApiException>(Actual).ConfigureAwait(true);
            Assert.Equal(expectedDetail, exception.Detail);
        }

        /// <summary>
        /// Client registry get demographics throws api patient exception given client registry records not found.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldThrowApiPatientExceptionGivenClientRegistryRecordsNotFound()
        {
            // Setup
            string expectedResponseCode = "BCHCIM.GD.2.0018";
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(expectedResponseCode, false, true);

            // Act
            async Task Actual()
            {
                await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn).ConfigureAwait(true);
            }

            // Verify
            ApiException exception = await Assert.ThrowsAsync<ApiException>(Actual).ConfigureAwait(true);
            Assert.Equal(ErrorMessages.ClientRegistryRecordsNotFound, exception.Detail);
        }

        /// <summary>
        /// Client registry get demographics throws api patient exception given client registry not returning person.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldThrowApiPatientExceptionGivenClientRegistryDoesNotReturnPerson()
        {
            // Setup
            string expectedResponseCode = "BCHCIM.GD.0.0099";
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(expectedResponseCode);

            // Act
            async Task Actual()
            {
                await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn).ConfigureAwait(true);
            }

            // Verify
            ApiException exception = await Assert.ThrowsAsync<ApiException>(Actual).ConfigureAwait(true);
            Assert.Equal(ErrorMessages.ClientRegistryDoesNotReturnPerson, exception.Detail);
        }

        /// <summary>
        /// Client registry get demographics throws api patient exception given client registry finds phn is invalid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldThrowApiPatientExceptionGivenClientRegistryPhnInvalid()
        {
            // Setup
            string expectedResponseCode = "BCHCIM.GD.2.0006";
            IClientRegistriesDelegate patientDelegate = GetClientRegistriesDelegate(expectedResponseCode, false, true);

            // Act
            async Task Actual()
            {
                await patientDelegate.GetDemographicsAsync(OidType.Phn, Phn).ConfigureAwait(true);
            }

            // Verify
            ApiException exception = await Assert.ThrowsAsync<ApiException>(Actual).ConfigureAwait(true);
            Assert.Equal(ErrorMessages.PhnInvalid, exception.Detail);
        }

        private static IClientRegistriesDelegate GetClientRegistriesDelegate(
            bool deceasedInd = false,
            bool noNames = false,
            bool noIds = false,
            bool throwsException = false)
        {
            return GetClientRegistriesDelegate(
                ResponseCode,
                deceasedInd,
                noNames,
                noIds,
                throwsException);
        }

        private static IClientRegistriesDelegate GetClientRegistriesDelegate(
            string expectedResponseCode = ResponseCode,
            bool deceasedInd = false,
            bool noNames = false,
            bool noIds = false,
            bool throwsException = false)
        {
            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget = GetSubjectTarget(deceasedInd, noNames, noIds);

            Mock<QUPA_AR101102_PortType> clientMock = new();

            if (throwsException)
            {
                clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>())).ThrowsAsync(new CommunicationException(string.Empty));
            }
            else
            {
                clientMock.Setup(x => x.HCIM_IN_GetDemographicsAsync(It.IsAny<HCIM_IN_GetDemographicsRequest>()))
                    .ReturnsAsync(
                        GetDemographics(subjectTarget, expectedResponseCode));
            }

            return new ClientRegistriesDelegate(
                new Mock<ILogger<ClientRegistriesDelegate>>().Object,
                clientMock.Object);
        }

        private static HCIM_IN_GetDemographicsResponseIdentifiedPerson GetSubjectTarget(bool deceasedInd = false, bool noNames = false, bool noIds = false)
        {
            return new HCIM_IN_GetDemographicsResponseIdentifiedPerson
            {
                id = new[]
                {
                    new II
                    {
                        root = "2.16.840.1.113883.3.51.1.1.6",
                        extension = Hdid,
                    },
                },
                identifiedPerson = new HCIM_IN_GetDemographicsResponsePerson
                {
                    deceasedInd = new BL
                        { value = deceasedInd },
                    id = GetIds(noIds),
                    name = GetNames(noNames),
                    birthTime = new TS
                    {
                        value = "20001231",
                    },
                    administrativeGenderCode = new CE
                    {
                        code = "F",
                    },
                },
            };
        }

        private static II[] GetIds(bool noIds = false)
        {
            if (noIds)
            {
                return Array.Empty<II>();
            }

            return new[]
            {
                new II
                {
                    root = "01010101010",
                    extension = Phn,
                },
            };
        }

        private static PN[] GetNames(bool noNames = false)
        {
            if (noNames)
            {
                return Array.Empty<PN>();
            }

            return new[]
            {
                new PN
                {
                    Items = new ENXP[]
                    {
                        new engiven
                        {
                            Text = new[] { FirstName },
                        },
                        new enfamily
                        {
                            Text = new[] { LastName },
                        },
                    },
                    use = new[] { cs_EntityNameUse.C },
                },
            };
        }

        private static HCIM_IN_GetDemographicsResponse1 GetDemographics(
            HCIM_IN_GetDemographicsResponseIdentifiedPerson subjectTarget,
            string expectedResponseCode = ResponseCode)
        {
            return new HCIM_IN_GetDemographicsResponse1
            {
                HCIM_IN_GetDemographicsResponse = new HCIM_IN_GetDemographicsResponse
                {
                    controlActProcess = new HCIM_IN_GetDemographicsResponseQUQI_MT120001ControlActProcess
                    {
                        queryAck = new HCIM_MT_QueryResponseQueryAck
                        {
                            queryResponseCode = new CS
                            {
                                code = expectedResponseCode,
                            },
                        },
                        subject = new[]
                        {
                            new HCIM_IN_GetDemographicsResponseQUQI_MT120001Subject2
                            {
                                target = subjectTarget,
                            },
                        },
                    },
                },
            };
        }
    }
}