using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GutenbergClient.net.Tests
{
    public class GutenbergClientTests
    {
        private Mock<HttpMessageHandler> mockMessageHandlerSuccess;

        [SetUp]
        public void Setup()
        {
            mockMessageHandlerSuccess = new Mock<HttpMessageHandler>();
            mockMessageHandlerSuccess.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(File.ReadAllText("data\\pg_catalog_10lines.txt"))
                });
        }

        [Test]
        public void CatalogRecordTest()
        {
            var record1 = GetPgCatalogRecordTemplate();
            Assert.IsNotNull(record1.Authors);

            Assert.IsTrue(record1.Authors.Count == 3, "Authors not loaded correctly");
            var author = record1.Authors[0];

            Assert.AreEqual("Hamilton, Alexander", author.LookupName);
            Assert.AreEqual("Alexander Hamilton", author.SimpleName);
            Assert.AreEqual(1757, author.BirthYear);
            Assert.AreEqual(1804, author.DeathYear);
            Assert.AreEqual(47, author.LifeSpan());


            // single author
            var record2 = GetPgCatalogRecordTemplate();
            record2.RawAuthors = "Coombs, Norman, 1932-";
            author = record2.Authors[0];
            Assert.AreEqual("Coombs, Norman", author.LookupName);
            Assert.AreEqual("Norman Coombs", author.SimpleName);
            Assert.AreEqual(1932, author.BirthYear);
            Assert.AreEqual(-1, author.DeathYear);
            Assert.AreEqual(-1, author.LifeSpan());

            // No author
            var record3 = GetPgCatalogRecordTemplate();
            record3.RawAuthors = string.Empty;
            Assert.IsNull(record3.Authors);

            // author with no dates
            var record4 = GetPgCatalogRecordTemplate();
            record4.RawAuthors = "Goodwin, John E.";
            Assert.IsNotNull(record4.Authors);
            author = record4.Authors[0];
            Assert.AreEqual("Goodwin, John E.", author.LookupName);
            Assert.AreEqual("John E. Goodwin", author.SimpleName);
            Assert.AreEqual(-1, author.BirthYear);
            Assert.AreEqual(-1, author.DeathYear);
            Assert.AreEqual(-1, author.LifeSpan());
        }

        [Test]
        public async Task CatalogLoadFromCacheTest()
        {
            var httpClient = new HttpClient(mockMessageHandlerSuccess.Object);
            var gc = GutenbergClient.GetClient(GutenbergConfig.DefaultConfig, httpClient);

            var tempFile = Path.Combine(Path.GetTempPath(), $"GutenbergCatalog-{DateTime.Now.ToString("yyyyMMddTHH")}.csv");
            await gc.CatalogDownloadAsync(tempFile);

            // assert file exists
            Assert.IsTrue(File.Exists(tempFile), $"Catalog File {tempFile} does not exist");

            var records = await gc.LoadCatalogFromCacheAsync(tempFile);

            Assert.IsNotNull(records);
            Assert.IsTrue(records.Count > 0, "failed to load catalog from cache");
        }


        [Test]
        public async Task CatalogLoadOnlineTest()
        {
            // real one
            var httpClient = new HttpClient();
            var gc = GutenbergClient.GetClient(GutenbergConfig.DefaultConfig, httpClient);

            var tempFile = $"GutenbergCatalog-real-{DateTime.Now.ToString("yyyyMMddTHH")}.csv";
            await gc.CatalogDownloadAsync(tempFile);

            // assert file exists
            Assert.IsTrue(File.Exists(@".\.gutenberg\" + tempFile), $"Catalog File {tempFile} does not exist");

            var records = await gc.LoadCatalogFromCacheAsync(tempFile);

            Assert.IsNotNull(records);
            Assert.IsTrue(records.Count > 0, "failed to load catalog from cache");
        }

        private static PgCatalogRecord GetPgCatalogRecordTemplate()
        {
            return new PgCatalogRecord(
                id: "1",
                type: "Text",
                title: "Hello world",
                rawIssued: "1994",
                languageCode: "en",
                rawAuthor: "Hamilton, Alexander, 1757-1804; Jay, John, 1745-1829; Madison, James, 1751-1836",
                loCC: "JK; KF",
                rawSubject: "Children's Literature; Poetry; Native America",
                rawBookshelves: "Best Books Ever Listings; Banned Books from Anne Haight's list; Banned Books List from the American Library Association"
                );
        }
    }
}