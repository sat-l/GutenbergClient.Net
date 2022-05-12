using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace GutenbergClient.net
{
    public class GutenbergCatalogIndex
    {
        public GutenbergCatalogIndex() { }

        private static GutenbergCatalogIndex instance;
        private static object lockObject;

        //public static GutenbergCatalogIndex GetInstance()
        //{
        //    if (instance == null)
        //    {
        //        lock (lockObject)
        //        {
        //            if (instance == null)
        //            {
        //                instance = new GutenbergCatalogIndex();
        //            }
        //        }
        //    }
        //    return instance;
        //}

        public int TotalBooks { get; private set; }

        public int TotalAuthors
        {
            get
            {
                return this.AuthorLookup.Count;
            }
        }

        public Dictionary<int, int> BooksByYear { get; private set; } = new Dictionary<int, int>();

        public Dictionary<string, int> BooksByGenre { get; private set; } = new Dictionary<string, int>();

        public Dictionary<char, int> BooksByAlphabet { get; private set; } = new Dictionary<char, int>();


        public void BuildIndex(PgCatalogRecord book)
        {
            this.TotalBooks++;

            if (book.Authors?.Any() == true)
            {
                foreach (var author in book.Authors)
                {
                    if (!this.AuthorLookup.ContainsKey(author.SimpleName))
                        this.AuthorLookup.Add(author.SimpleName, new List<PgCatalogRecord>());

                    this.AuthorLookup[author.SimpleName].Add(book);
                }
            }

            if (book.IssuedDate != null)
            {
                var year = book.IssuedDate.Value.Year;
                if (!this.BooksByYear.ContainsKey(year))
                    this.BooksByYear.Add(year, 0);

                this.BooksByYear[year]++;
            }

            if (book.BookShelves?.Any() == true)
            {
                foreach (var bookshelf in book.BookShelves)
                {
                    if (!this.BooksByGenre.ContainsKey(bookshelf))
                        this.BooksByGenre.Add(bookshelf, 0);

                    this.BooksByGenre[bookshelf]++;
                }
            }

            var firstChar = book.Title.FirstOrDefault();
            if (firstChar != '\0')
            {
                if (!this.BooksByAlphabet.ContainsKey(firstChar))
                    this.BooksByAlphabet.Add(firstChar, 0);
                this.BooksByAlphabet[firstChar]++;
            }
        }


        public Dictionary<string, IList<PgCatalogRecord>> AuthorLookup = new Dictionary<string, IList<PgCatalogRecord>>();

        public void TrimIndex()
        {
            var tempBooksByGenre = new Dictionary<string, int>(this.BooksByGenre.Count);
            foreach (var bbg in this.BooksByGenre)
            {
                if (bbg.Value > 10)
                {
                    tempBooksByGenre[bbg.Key] = bbg.Value;
                }
            }
            this.BooksByGenre.Clear();
            this.BooksByGenre = tempBooksByGenre;
        }
    }
}
