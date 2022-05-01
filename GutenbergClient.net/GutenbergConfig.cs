using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GutenbergClient.net
{
    public sealed class GutenbergConfig
    {
        public Uri GutenbergUrl { get; set; }

        public Uri CatalogCsvUrl
        {
            get
            {
                return new Uri(GutenbergUrl, gutenbergFeedPath + pg_catalog_csv);
            }
        }

        public static readonly GutenbergConfig DefaultClient = new GutenbergConfig();

        #region Gutenberg Site constsants
        private const string defaultGutenbergUrl = "https://www.gutenberg.org/";
        private const string userAgent = "";
        private const string gutenbergFeedPath = "cache/epub/feeds/";
        private const string pg_catalog_csv = "pg_catalog.csv";
        private const string today_rss = "today.rss";
        #endregion Gutenberg Site constsants

        public GutenbergConfig()
            : this(defaultGutenbergUrl)
        {
        }

        public GutenbergConfig(string gutenbergUrl)
        {
            this.GutenbergUrl = new Uri(defaultGutenbergUrl);
        }
    }
}
