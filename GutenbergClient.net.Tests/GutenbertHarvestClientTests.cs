using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GutenbergClient.net.Tests
{
    public class GutenbertHarvestClientTests
    {
        private Mock<HttpMessageHandler> mockHarvestResponseHandler;
        private Mock<HttpMessageHandler> mockBookContentHandler;

        [SetUp]
        public void Setup()
        {
            mockHarvestResponseHandler = new Mock<HttpMessageHandler>();
            mockHarvestResponseHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(File.ReadAllText("data\\harvestdata.txt"))
                });

            mockBookContentHandler = new Mock<HttpMessageHandler>();
            mockBookContentHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("My test book!!")
                });
        }


        [Test]
        [Order(1)]
        public async Task HarvestClientTest()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"GutenbergHarvest-{DateTime.Now.ToString("yyyyMMdd")}.txt");

            var httpClient = new HttpClient(mockHarvestResponseHandler.Object);
            var hClient = GutenbergHarvestClient.GetClient(GutenbergConfig.DefaultConfig, httpClient: httpClient);
            await hClient.DownloadLocalHarvestCacheAsync();

        }


        [Test]
        [Order(2)]
        public async Task DownloadBookByIdTest()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"GutenbergHarvest-{DateTime.Now.ToString("yyyyMMdd")}.txt");

            var httpClient = new HttpClient(mockBookContentHandler.Object);
            var hClient = GutenbergHarvestClient.GetClient(GutenbergConfig.DefaultConfig, httpClient: httpClient);

            var localFile = @".\12383.txt";
            await hClient.DownloadBookById("12383", localFile);

            Assert.IsTrue(File.Exists(localFile));

            var content = File.ReadAllText(localFile);
            Assert.AreEqual("My test book!!", content);
        }


        [Test]
        [Ignore("temp")]
        public void TempThrowAwayTests()
        {
            var harvestFile = @"C:\Users\satishl\AppData\Local\Temp\GutenbergHarvest-20220502T09.txt";
            using (var sr = new StreamReader(harvestFile))
            {
                int count = 0;
                var ht = new HashSet<string>();
                while (sr.EndOfStream != true)
                {
                    var line = sr.ReadLine();
                    var tokens = line.Split('/');
                    if (tokens.Length <= 1)
                        continue;

                    var file = tokens[tokens.Length - 2];
                    //Trace.WriteLine(file);
                    ht.Add(file);
                }

                Trace.WriteLine("TOtal files: " + ht.Count);
            }
        }
    }
}
