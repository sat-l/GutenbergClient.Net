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

        private const string localCatalogFile = ".localCatalogFile.txt";

        private GutenbergCatalogIndex catalogIndex;

        public GutenbergCatalogIndex CatalogIndex { get { return catalogIndex; } }

        public static GutenbergClient GetClient()
        {
            // defaults
            return GetClient(GutenbergConfig.DefaultConfig,
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
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", this.config.HttpUserAgent);

            this.catalogIndex = new GutenbergCatalogIndex();
        }

        #region Catalog Apis

        public async Task CatalogDownloadAsync(string localCatalogFile = localCatalogFile)
        {
            var catalogUri = config.CatalogCsvUrl;

            var downloadFileFullPath = Path.Combine(config.LocalCacheDirectory, localCatalogFile);

            var response = await this.httpClient.GetAsync(catalogUri);
            using (var fs = new FileStream(downloadFileFullPath, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        public async Task<IList<PgCatalogRecord>> LoadCatalogFromCacheAsync(string localCatalogFile = localCatalogFile,
            bool doNotIndex = false)
        {
            var localCacheFileFullPath = Path.Combine(config.LocalCacheDirectory, localCatalogFile);
            var result = new List<PgCatalogRecord>();

            using (var reader = new StreamReader(localCacheFileFullPath))
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

                    if (!doNotIndex)
                    {
                        this.catalogIndex.BuildIndex(catalogItem);
                    }
                }
            }

            this.catalogIndex.TrimIndex();

            return result;
        }

        public async Task<IList<PgCatalogRecord>> LoadCatalogOnlineAsync()
        {
            var localFile = $"GutenbergCatalog-{DateTime.Now.ToString("yyyyMMddTHH")}.csv";
            await CatalogDownloadAsync(localFile);

            return await LoadCatalogFromCacheAsync(localFile);
        }

        #endregion Catalog Apis


        #region Catalog Search Apis


        public async Task<IList<string>> SearchByAuthor(string authorQuerystring)
        {
            var results = new List<string>();

            foreach (var author in this.catalogIndex.AuthorLookup.Keys)
            {
                if (author.Contains(authorQuerystring, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(author);
                }
            }

            return await Task.FromResult(results);
        }

        #endregion Catalog Search Apis

    }
}
