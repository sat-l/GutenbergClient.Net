using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GutenbergClient.net
{
    public class GutenbergClient
    {
        private HttpClient httpClient;
        private GutenbergConfig config;

        //public delegate void CatalogDownloadedEvent(string downloadPath);
        //public event CatalogDownloadedEvent OnCatalogDownloaded;


        public static GutenbergClient GetClient()
        {
            // defaults
            return GetClient(GutenbergConfig.DefaultClient,
                new HttpClient());
        }

        public static GutenbergClient GetClient(GutenbergConfig config)
        {
            return GetClient(config, new HttpClient());
        }

        public static GutenbergClient GetClient(GutenbergConfig config,
            HttpClient httpClient)
        {
            // Check if we need to make this singleton
            return new GutenbergClient(config, httpClient);
        }

        private GutenbergClient(GutenbergConfig config,
            HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.config = config;

            // set user agent for scraping
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36");


        }

        #region Catalog Apis

        public async Task CatalogDownloadAsync(string downloadFile)
        {
            var catalogUri = config.CatalogCsvUrl;

            var response = await this.httpClient.GetAsync(catalogUri);
            using (var fs = new FileStream(downloadFile, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        public async Task<IList<PgCatalogRecord>> LoadCatalogFromCacheAsync(string localCacheFile)
        {
            var result = new List<PgCatalogRecord>();

            using (var reader = new StreamReader(localCacheFile))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                while (await csv.ReadAsync())
                {
                    var id = csv.GetField<string>("Text#");
                    var type = csv.GetField<string>("Type");
                    var rawIssued = csv.GetField<string>("Issued");
                    var languageCode = csv.GetField<string>("Language");
                    var title = csv.GetField<string>("Title");
                    var rawAuthor = csv.GetField<string>("Authors");
                    var rawSubject = csv.GetField<string>("Subjects");
                    var loCC = csv.GetField<string>("LoCC");
                    var rawBookshelves = csv.GetField<string>("Bookshelves");

                    var catalogItem = new PgCatalogRecord(
                        id: id,
                        type: type,
                        title: title,
                        languageCode: languageCode,
                        rawIssued: rawIssued,
                        rawAuthor: rawAuthor,
                        rawSubject: rawSubject,
                        loCC: loCC,
                        rawBookshelves: rawBookshelves);

                    result.Add(catalogItem);
                }
            }

            return result;
        }

        public async Task<IList<PgCatalogRecord>> LoadCatalogOnlineAsync()
        {
            var localFile = $"GutenbergCatalog-{DateTime.Now.ToString("yyyyMMddTHH")}.csv";
            await CatalogDownloadAsync(localFile);

            return await LoadCatalogFromCacheAsync(localFile);
        }

        #endregion Catalog Apis


        #region Harvest Apis

        #endregion Harvest Apis
    }
}
