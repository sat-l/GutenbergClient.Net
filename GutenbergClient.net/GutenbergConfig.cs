using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GutenbergClient.net
{
    public sealed class GutenbergConfig
    {
        public string GutenbergUrl { get; set; }

        public Uri CatalogCsvUrl
        {
            get
            {
                return new Uri(GutenbergUrl + gutenbergFeedPath + pg_catalog_csv);
            }
        }

        public Uri RobotUrl
        {
            get
            {
                return new Uri(GutenbergUrl + RobotUrl);
            }
        }


        /// <summary>
        /// Get Harvest url
        /// </summary>
        /// <returns></returns>
        public string GetHarvestUrl(string harvestPathWithFilter)
        {
            var url = $"{defaultGutenbergUrl}{robotPath}{harvestPath}";
            if (!string.IsNullOrWhiteSpace(harvestPathWithFilter))
                url += harvestPathWithFilter;

            return url;
        }

        public string LocalCacheDirectory { get; private set; }


        public string HttpUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36";


        public static readonly GutenbergConfig DefaultConfig = new GutenbergConfig();

        #region Gutenberg Site constants
        private const string defaultGutenbergUrl = "https://www.gutenberg.org/";
        private const string userAgent = "";
        private const string gutenbergFeedPath = "cache/epub/feeds/";
        private const string pg_catalog_csv = "pg_catalog.csv";
        private const string today_rss = "today.rss";

        private const string robotPath = "robot/";
        private const string harvestPath = "harvest/";
        private const string fileTypesQueryParam = "?filetypes[]=";

        #endregion Gutenberg Site constants

        public GutenbergConfig(string localcCacheDirectory = @".\.gutenberg\")
        {
            this.GutenbergUrl = defaultGutenbergUrl;
            this.LocalCacheDirectory = localcCacheDirectory;

            if (!Directory.Exists(this.LocalCacheDirectory))
            {
                Directory.CreateDirectory(this.LocalCacheDirectory);
            }
        }
    }
}
