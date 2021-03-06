﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Wapice.IoTTicket.RestClient.Exceptions;
using Wapice.IoTTicket.RestClient.Model;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Wapice.IoTTicket.RestClient.Tests
{

    [TestClass]
    public class IoTTicketClientDeviceTests: WireMockTestBase
    {

        DateTime DEVICE_CREATED_AT = new DateTime(2019, 1, 1, 0, 0, 0, 0);
        const string DEVICE_DEVICE_ID = "deviceId";
        const string DEVICE_HREF = "https://my.iot-ticket.com/api/v1/devices/b3076fd2dd514397a19fd24fd07bf7e1";
        const string DEVICE_NAME = "name";
        const string DEVICE_MANUFACTURER = "manufacturer";
        const string DEVICE_TYPE = "type";
        const string DEVICE_DESCRIPTION = "description";

        const string DEVICE_ATTRIBUTE_KEY = "key";
        const string DEVICE_ATTRIBUTE_VALUE = "value";

        [TestMethod]
        public async Task TestRegisterDeviceAsync_ValidDevice_ExpectedRequestReceived()
        {
            Device device = CreateValidDevice();
            DeviceDetails deviceDetails = CreateValidDeviceDetails();

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/")
                .WithHeader("Accept", "application/json")
                .WithBody(new JsonMatcher(JsonSerializer.Serialize(device)))
                .UsingPost();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.Created)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceDetails)));

            await Client.RegisterDeviceAsync(device);
            AssertReceivedAsOnlyRequest(expectedRequest);
        }

        [TestMethod]
        public void TestRegisterDeviceAsync_ValidDevice_DeviceDetailsIsDeserializedCorrectly()
        {
            Device device = CreateValidDevice();
            DeviceDetails deviceDetails = CreateValidDeviceDetails();

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/")
                .WithHeader("Accept", "application/json")
                .WithBody(new JsonMatcher(JsonSerializer.Serialize(device)))
                .UsingPost();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.Created)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceDetails)));

            var result = Client.RegisterDeviceAsync(device).Result;
            AssertDeviceDetailsAreEqual(deviceDetails, result);
            
        }

        [TestMethod]
        public async Task TestRegisterDeviceAsync_MissingManufacturerField_IoTServerCommunicationExceptionIsThrownWithBadRequest()
        {
            var deviceWithMissingManufacturer = new Device
            {
                Name = "name"
            };

            var errorInfo = new ErrorInfo
            {
                Description = "DeviceManufacturer is needed",
                Code = 8003,
                MoreInfoUrl = new Uri("https://my.iot-ticket.com/api/v1/errorcodes"),
                ApiVersion = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/")
                .WithHeader("Accept", "application/json")
                .WithBody(new JsonMatcher(JsonSerializer.Serialize(deviceWithMissingManufacturer)))
                .UsingPost();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(errorInfo)));

            try
            {
                await Client.RegisterDeviceAsync(deviceWithMissingManufacturer);
                Assert.Fail("Expected " + nameof(IoTServerCommunicationException));
            }
            catch (IoTServerCommunicationException actualException)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, actualException.HttpStatusCode);
            }
        }

        [TestMethod]
        public async Task TestRegisterDeviceAsync_MissingManufacturerField_ErrorInfoIsDeserializedSuccesfully()
        {
            var deviceWithMissingManufacturer = new Device
            {
                Name = "name"
            };

            var errorInfo = new ErrorInfo
            {
                Description = "DeviceManufacturer is needed",
                Code = 8003,
                MoreInfoUrl = new Uri("https://my.iot-ticket.com/api/v1/errorcodes"),
                ApiVersion = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/")
                .WithHeader("Accept", "application/json")
                .WithBody(new JsonMatcher(JsonSerializer.Serialize(deviceWithMissingManufacturer)))
                .UsingPost();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(errorInfo)));

            try
            {
                await Client.RegisterDeviceAsync(deviceWithMissingManufacturer);
                Assert.Fail("Expected " + nameof(IoTServerCommunicationException));
            }
            catch (IoTServerCommunicationException expectedException)
            {
                Assert.AreEqual(errorInfo.ApiVersion, expectedException?.ErrorInfo?.ApiVersion);
                Assert.AreEqual(errorInfo.Code, expectedException?.ErrorInfo?.Code);
                Assert.AreEqual(errorInfo.Description, expectedException?.ErrorInfo?.Description);
                Assert.AreEqual(errorInfo.MoreInfoUrl, expectedException?.ErrorInfo?.MoreInfoUrl);
            }
        }

        [TestMethod]
        public async Task TestGetDevicesAsync_DevicesFound_ExpectedRequestReceived()
        {
            const int LIMIT = 2;
            const int OFFSET = 1;
            DeviceDetails deviceDetails = CreateValidDeviceDetails();

            PagedResult<DeviceDetails> response = new PagedResult<DeviceDetails>()
            {
                RequestedCount = LIMIT,
                Skip = OFFSET,
                Result = new List<DeviceDetails>
                {
                    deviceDetails
                },
                TotalCount = 1
               
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/")
                .WithHeader("Accept", "application/json")
                .WithParam("limit", LIMIT.ToString())
                .WithParam("offset", OFFSET.ToString())
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceDetails)));

            await Client.GetDevicesAsync(LIMIT, OFFSET);

            AssertReceivedAsOnlyRequest(expectedRequest);
        }

        [TestMethod]
        public void TestGetDevicesAsync_DevicesFound_DeviceDetailsPagedResultDeserializedSuccessfully()
        {
            const int LIMIT = 2;
            const int OFFSET = 1;
            DeviceDetails deviceDetails = CreateValidDeviceDetails();

            PagedResult<DeviceDetails> deviceDetailsPagedResult = new PagedResult<DeviceDetails>()
            {
                RequestedCount = LIMIT,
                Skip = OFFSET,
                Result = new List<DeviceDetails>
                {
                    deviceDetails
                },
                TotalCount = 1

            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/")
                .WithHeader("Accept", "application/json")
                .WithParam("limit", LIMIT.ToString())
                .WithParam("offset", OFFSET.ToString())
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceDetailsPagedResult)));

            var result = Client.GetDevicesAsync(LIMIT, OFFSET).Result;
            Assert.AreEqual(deviceDetailsPagedResult.Skip, result.Skip);
            Assert.AreEqual(deviceDetailsPagedResult.RequestedCount, result.RequestedCount);
            Assert.AreEqual(deviceDetailsPagedResult.TotalCount, result.TotalCount);
            Assert.AreEqual(deviceDetailsPagedResult.Result.Count(), result?.Result?.Count());

            AssertDeviceDetailsAreEqual(deviceDetailsPagedResult.Result.First(), result?.Result?.First());
        }

        [TestMethod]
        public async Task TestGetDeviceAsync_DeviceExists_ExpectedRequestReceived()
        {
            DeviceDetails deviceDetails = CreateValidDeviceDetails();
            var deviceId = deviceDetails.Id;

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/" + deviceId + "/")
                .WithHeader("Accept", "application/json")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceDetails)));

            await Client.GetDeviceAsync(deviceId);

            AssertReceivedAsOnlyRequest(expectedRequest);
        }

        [TestMethod]
        public void TestGetDeviceAsync_DeviceExists_DeviceDetailsDeserializedCorrectly()
        {
            DeviceDetails deviceDetails = CreateValidDeviceDetails();
            var deviceId = deviceDetails.Id;

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/" + deviceId + "/")
                .WithHeader("Accept", "application/json")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceDetails)));

            var result = Client.GetDeviceAsync(deviceId).Result;

            AssertDeviceDetailsAreEqual(deviceDetails, result);
        }

        [TestMethod]
        public async Task TestGetDeviceAsync_DeviceIsNotFound_IoTServerCommunicationExceptionIsThrownWithForbiddenStatus()
        {
            const string NOT_EXISTING_DEVICE_ID = "id2";

            var errorInfo = new ErrorInfo
            {
                Description = "Re-check device id and ensure device access is valid",
                Code = 8001,
                MoreInfoUrl = new Uri("https://my.iot-ticket.com/api/v1/errorcodes"),
                ApiVersion = 1,
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/" + NOT_EXISTING_DEVICE_ID + "/")
                .WithHeader("Accept", "application/json")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.Forbidden)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(errorInfo)));

            try
            {
                await Client.GetDeviceAsync(NOT_EXISTING_DEVICE_ID);
                Assert.Fail("Expected " + nameof(IoTServerCommunicationException));
            }
            catch (IoTServerCommunicationException actualException)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, actualException.HttpStatusCode);
            }
        }

        [TestMethod]
        public async Task TestGetDeviceAsync_DeviceIsNotFound_ErrorInfoIsDeserializedCorrectly()
        {
            const string NOT_EXISTING_DEVICE_ID = "id2";

            var errorInfo = new ErrorInfo
            {
                Description = "Re-check device id and ensure device access is valid",
                Code = 8001,
                MoreInfoUrl = new Uri("https://my.iot-ticket.com/api/v1/errorcodes"),
                ApiVersion = 1,
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/devices/" + NOT_EXISTING_DEVICE_ID + "/")
                .WithHeader("Accept", "application/json")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.Forbidden)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(errorInfo)));

            try
            {
                await Client.GetDeviceAsync(NOT_EXISTING_DEVICE_ID);
                Assert.Fail("Expected " + nameof(IoTServerCommunicationException));
            }
            catch (IoTServerCommunicationException actualException)
            {
                Assert.AreEqual(errorInfo.ApiVersion, actualException?.ErrorInfo?.ApiVersion);
                Assert.AreEqual(errorInfo.Code, actualException?.ErrorInfo?.Code);
                Assert.AreEqual(errorInfo.Description, actualException?.ErrorInfo?.Description);
                Assert.AreEqual(errorInfo.MoreInfoUrl, actualException?.ErrorInfo?.MoreInfoUrl);
            }
        }

        private DeviceDetails CreateValidDeviceDetails()
        {
            var deviceAttributes = new List<DeviceAttribute>
            {
                new DeviceAttribute(DEVICE_ATTRIBUTE_KEY, DEVICE_ATTRIBUTE_VALUE)
            };

            var deviceDetails = new DeviceDetails
            {
                CreationDate = DEVICE_CREATED_AT,
                Id = DEVICE_DEVICE_ID,
                Url = new Uri(DEVICE_HREF),
                Attributes = deviceAttributes,
                Manufacturer = DEVICE_MANUFACTURER,
                Name = DEVICE_NAME
            };
            return deviceDetails;
        }

        private Device CreateValidDevice()
        {
            var deviceAttributes = new List<DeviceAttribute>
            {
                new DeviceAttribute(DEVICE_ATTRIBUTE_KEY, DEVICE_ATTRIBUTE_VALUE)
            };


            var device = new Device
            {
                Name = DEVICE_NAME,
                Type = DEVICE_TYPE,
                Attributes = deviceAttributes,
                Description = DEVICE_DESCRIPTION,
                Manufacturer = DEVICE_MANUFACTURER
            };

            return device;
        }

        private void AssertDeviceDetailsAreEqual(DeviceDetails expected, DeviceDetails actual)
        {
            Assert.AreEqual(expected.CreationDate, actual?.CreationDate);
            Assert.AreEqual(expected.Id, actual?.Id);
            Assert.AreEqual(expected.Url, actual?.Url);
            Assert.AreEqual(expected.Manufacturer, actual?.Manufacturer);
            Assert.AreEqual(expected.Name, actual?.Name);

            Assert.AreEqual(expected.Attributes.Count(), actual?.Attributes?.Count());
            Assert.AreEqual(expected.Attributes.First().Key, actual?.Attributes?.First()?.Key);
            Assert.AreEqual(expected.Attributes.First().Value, actual?.Attributes?.First()?.Value);
            

        }

    }
}
