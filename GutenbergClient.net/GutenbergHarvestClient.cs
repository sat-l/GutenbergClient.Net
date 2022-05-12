using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GutenbergClient.net
{
    public sealed class GutenbergHarvestClient
    {
        private readonly HttpClient httpClient;

        private readonly string localCacheFilePath;

        private const string harvestLinksFile = ".harvestlinks.txt";

        private readonly Dictionary<string, string> idToBookLookup;
        private GutenbergConfig config;

        private GutenbergHarvestClient(GutenbergConfig config, HttpClient httpClient)
        {
            this.config = config;
            this.httpClient = httpClient;
            this.idToBookLookup = new Dictionary<string, string>();
            this.localCacheFilePath = Path.Combine(config.LocalCacheDirectory, harvestLinksFile);

            // load the books if the cache is already present
            if (File.Exists(localCacheFilePath))
            {
                using (var sr = new StreamReader(localCacheFilePath))
                {
                    while (sr.EndOfStream != true)
                    {
                        var line = sr.ReadLine();
                        var bookId = string.Empty;

                        if (GetIdFromBookLink(line, out bookId))
                        {
                            this.idToBookLookup[bookId] = line;
                        }
                    }
                }
            }
        }


        public static GutenbergHarvestClient GetClient()
        {
            return GetClient(GutenbergConfig.DefaultConfig, new HttpClient());
        }

        public static GutenbergHarvestClient GetClient(GutenbergConfig config)
        {
            return GetClient(config, new HttpClient());
        }


        public static GutenbergHarvestClient GetClient(GutenbergConfig config, HttpClient httpClient)
        {
            return new GutenbergHarvestClient(config, httpClient);
        }

        /// <summary>
        /// downloads harvest file links to a local cache file.
        /// </summary>
        /// <param name="fileType">
        /// could be txt/html/rdf. example: https://www.gutenberg.org/robot/harvest?offset=40530&filetypes[]=txt. 
        /// empty value would include all file types.
        /// </param>
        /// <returns>task </returns>
        public async Task DownloadLocalHarvestCacheAsync(string fileType = "txt")
        {
            string harvestUrl = config.GetHarvestUrl("harvest&filetypes[]=" + fileType);

            this.idToBookLookup.Clear();

            var parser = new HtmlParser();
            string bookId = string.Empty;

            using (var fs = File.CreateText(this.localCacheFilePath))
            using (var csv = new CsvWriter(fs, CultureInfo.InvariantCulture))
            {
                while (true)
                {
                    var response = await httpClient.GetAsync(harvestUrl);
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    using (var doc = parser.ParseDocument(stringResponse))
                    {
                        var hrefLinks = doc.QuerySelectorAll("a").OfType<IHtmlAnchorElement>().ToList();
                        if (!hrefLinks.Any())
                            break;

                        for (int i = 0; i < hrefLinks.Count - 1; i++)
                        {
                            var href = hrefLinks[i].Href;
                            //if (!href.Contains("-8"))
                            {
                                fs.WriteLine(href);
                                if (GetIdFromBookLink(href, out bookId))
                                {
                                    this.idToBookLookup[bookId] = href;
                                }
                            }
                        }

                        var nextPageLink = hrefLinks.Last();
                        if (nextPageLink?.Text.Equals("Next Page") != true)
                        {
                            break;
                        }
                        var nextPageUrlPath = nextPageLink.Href.Replace("about:///", string.Empty);
                        harvestUrl = config.GetHarvestUrl(nextPageUrlPath);
                    }
                }
            }
        }

        public async Task DownloadBookById(string bookId, string outputFileName)
        {
            if (this.idToBookLookup.Count == 0)
            {
                throw new InvalidDataException($"Local Harvest file {this.localCacheFilePath} is empty. Ensure you have called [DownloadLocalHarvestCacheAsync] Api before using this api.");
            }

            if (string.IsNullOrWhiteSpace(bookId)
                || !this.idToBookLookup.ContainsKey(bookId))
                throw new ArgumentException(nameof(bookId));

            string bookUrl = this.idToBookLookup[bookId];
            var response = await this.httpClient.GetAsync(bookUrl);

            var outputFilePath = Path.Combine(config.LocalCacheDirectory, outputFileName);
            using (var fs = new FileStream(outputFilePath, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        private static bool GetIdFromBookLink(string bookLink, out string id)
        {
            id = string.Empty;
            var tokens = bookLink.Split('/');
            if (tokens.Length <= 1)
                return false;

            id = tokens[tokens.Length - 2];
            return true;
        }
    }
}
