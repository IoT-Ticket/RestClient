﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wapice.IoTTicket.RestClient.Exceptions;
using Wapice.IoTTicket.RestClient.Model;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Wapice.IoTTicket.RestClient.Tests
{
    [TestClass]
    public class IoTTicketClientQuotaTests : WireMockTestBase
    {

        [TestMethod]
        public async Task TestGetQuotaAsync_QuotaIsFound_ExpectedRequestIsReceived()
        {
            var quota = new Quota
            {
                TotalDeviceCount = 1,
                MaxDeviceCount = 1,
                MaxDataNodeCountPerDevice = 1,
                UsedStorageBytes = 1,
                MaxStorageBytes = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/quota/all/")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create().
                WithStatusCode(HttpStatusCode.OK).
                WithHeader("Content-Type", "application/json").
                WithBody(JsonSerializer.Serialize(quota)));

            await Client.GetQuotaAsync();

            AssertReceivedAsOnlyRequest(expectedRequest);
        }

        [TestMethod]
        public void TestGetQuotaAsync_QuotaIsFound_QuotaIsDeserializedSuccessfully()
        {
            var quota = new Quota
            {
                TotalDeviceCount = 1,
                MaxDeviceCount = 1,
                MaxDataNodeCountPerDevice = 1,
                UsedStorageBytes = 1,
                MaxStorageBytes = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/quota/all/")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(quota)));

            Quota result = Client.GetQuotaAsync().Result;

            Assert.AreEqual(quota.TotalDeviceCount, result?.TotalDeviceCount);
            Assert.AreEqual(quota.MaxDeviceCount, result?.MaxDeviceCount);
            Assert.AreEqual(quota.MaxDataNodeCountPerDevice, result?.MaxDataNodeCountPerDevice);
            Assert.AreEqual(quota.UsedStorageBytes, result?.UsedStorageBytes);
            Assert.AreEqual(quota.MaxStorageBytes, result?.MaxStorageBytes);
        }

        [TestMethod]
        public void TestGetDeviceQuotaAsync_DeviceQuotaWithGivenDeviceIdIsFound_ExpectedRequestReceived()
        {
            const string existingDeviceId = "id1";

            var deviceQuota = new DeviceQuota
            {
                DeviceId = existingDeviceId,
                RequestCountToday = 1,
                MaxReadRequestCountPerDay = 1,
                DataNodeCount = 1,
                UsedStorageBytes = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/quota/" + existingDeviceId + "/")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceQuota)));

            var result = Client.GetDeviceQuotaAsync(existingDeviceId).Result;

            AssertReceivedAsOnlyRequest(expectedRequest);
        }

        [TestMethod]
        public async Task TestGetDeviceQuotaAsync_DeviceQuotaWithGivenDeviceIdIsFound_DeviceQuotaDeserializedSuccessfully()
        {
            const string existingDeviceId = "id1";

            var deviceQuota = new DeviceQuota
            {
                DeviceId = existingDeviceId,
                RequestCountToday = 1,
                MaxReadRequestCountPerDay = 1,
                DataNodeCount = 1,
                UsedStorageBytes = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/quota/" + existingDeviceId + "/")
                .UsingGet();

            Server
                .Given(expectedRequest)
                .RespondWith(Response
                .Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(deviceQuota)));

            await Client.GetDeviceQuotaAsync(existingDeviceId);

            AssertReceivedAsOnlyRequest(expectedRequest);
        }

        [TestMethod]
        public async Task TestGetDeviceQuotaAsync_DeviceQuotaWithGivenDeviceIdIsNotFound_IoTServerCommunicationExceptionIsThrownWithForbiddenStatus()
        {
            const string notExistingDeviceId = "id2";

            var errorInfo = new ErrorInfo
            {
                Description = "Re-check device id and ensure device access is valid",
                Code = 8001,
                MoreInfoUrl = new Uri("https://my.iot-ticket.com/api/v1/errorcodes"),
                ApiVersion = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/quota/" + notExistingDeviceId + "/")
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
                await Client.GetDeviceQuotaAsync(notExistingDeviceId);
                Assert.Fail(nameof(IoTServerCommunicationException) + "was not thrown");
            } catch (IoTServerCommunicationException expectedException)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, expectedException.HttpStatusCode);
            }

            AssertReceivedAsOnlyRequest(expectedRequest);
        }

        [TestMethod]
        public async Task TestGetDeviceQuotaAsync_DeviceQuotaWithGivenDeviceIdIsNotFound_ErrorInfoIsDeserializedCorrectly()
        {
            const string notExistingDeviceId = "id2";

            var errorInfo = new ErrorInfo
            {
                Description = "Re-check device id and ensure device access is valid",
                Code = 8001,
                MoreInfoUrl = new Uri("https://my.iot-ticket.com/api/v1/errorcodes"),
                ApiVersion = 1
            };

            var expectedRequest =
                Request
                .Create()
                .WithPath("/quota/" + notExistingDeviceId + "/")
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
                await Client.GetDeviceQuotaAsync(notExistingDeviceId);
                Assert.Fail(nameof(IoTServerCommunicationException) + "was not thrown");
            }
            catch (IoTServerCommunicationException actualException)
            {
                Assert.AreEqual(errorInfo.ApiVersion, actualException?.ErrorInfo?.ApiVersion);
                Assert.AreEqual(errorInfo.Code, actualException?.ErrorInfo?.Code);
                Assert.AreEqual(errorInfo.Description, actualException?.ErrorInfo?.Description);
                Assert.AreEqual(errorInfo.MoreInfoUrl, actualException?.ErrorInfo?.MoreInfoUrl);
            }

            AssertReceivedAsOnlyRequest(expectedRequest);
        }
    }
}
