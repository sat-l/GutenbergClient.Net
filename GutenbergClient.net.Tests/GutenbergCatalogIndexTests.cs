using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GutenbergClient.net.Tests
{
    public class GutenbergCatalogIndexTests
    {
        private const string catalogFull = @".\data\GutenbergCatalog-full.csv";
        private const string catalogPartial = @".\data\pg_catalog_10lines.txt";
        private const string localcacheDir = @".\.gutenberg\";

        [SetUp]
        public void Setup()
        {
            if (!Directory.Exists(localcacheDir))
            {
                Directory.CreateDirectory(localcacheDir);
            }
            File.Copy(catalogPartial, localcacheDir + Path.GetFileName(catalogPartial), true);
            File.Copy(catalogFull, localcacheDir + Path.GetFileName(catalogFull), true);
        }

        [Test]
        public async Task CheckIndexForPartialCatalog()
        {
            var gc = GutenbergClient.GetClient(GutenbergConfig.DefaultConfig);
            await gc.LoadCatalogFromCacheAsync(Path.GetFileName(catalogPartial));

            Assert.AreEqual(10, gc.CatalogIndex.TotalBooks);
            Assert.AreEqual(5, gc.CatalogIndex.TotalAuthors);
        }

        [Test]
        public async Task CheckIndexForFullCatalog()
        {
            var gc = GutenbergClient.GetClient(GutenbergConfig.DefaultConfig);

            var sw = Stopwatch.StartNew();
            await gc.LoadCatalogFromCacheAsync(Path.GetFileName(catalogFull));
            sw.Stop();

            Trace.WriteLine(sw.Elapsed.TotalSeconds);

            Trace.WriteLine($"Authors: {gc.CatalogIndex.TotalAuthors}, Titles: {gc.CatalogIndex.TotalBooks}, Genres: {gc.CatalogIndex.BooksByGenre.Count}");

            //Authors: 31975, Titles: 67883, Genres: 225

            Assert.AreEqual(67883, gc.CatalogIndex.TotalBooks);
            Assert.AreEqual(31975, gc.CatalogIndex.TotalAuthors);
            Assert.AreEqual(225, gc.CatalogIndex.BooksByGenre.Count);

        }

    }
}
