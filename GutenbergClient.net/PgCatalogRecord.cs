using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GutenbergClient.net
{
    public class PgCatalogRecord
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string RawIssued { get; set; }

        public string RawAuthors { get; set; }
        public string RawSubject { get; }

        public string Title { get; set; }

        public string LanguageCode { get; set; }

        public string LoCC { get; set; }

        public string RawBookshelves { get; }

        public DateTime AuthorBirthYear { get; set; }
        public DateTime AuthorDeathYear { get; set; }
        public string[] Subjects { get; set; }
        public string[] BookShelves { get; set; }

        public DateTime? IssuedDate { get; private set; }


        // Lazy Load 
        private List<AuthorInfo> authors;
        public List<AuthorInfo> Authors
        {
            get
            {
                // thread safety                
                if (authors == null)
                {
                    lock (this)
                    {
                        if (authors == null)
                        {
                            authors = GetAuthorDetails();
                        }
                    }
                }
                return authors;
            }
        }

        public PgCatalogRecord(string id,
            string type,
            string title,
            string rawIssued,
            string languageCode,
            string rawAuthor,
            string rawSubject,
            string loCC,
            string rawBookshelves)
        {
            this.Id = id;
            this.Type = type;
            this.Title = title;
            this.RawIssued = rawIssued;
            this.LanguageCode = languageCode;
            this.RawAuthors = rawAuthor;
            this.RawSubject = rawSubject;
            this.LoCC = loCC;
            this.RawBookshelves = rawBookshelves;

            // inferred
            DateTime issuedDate = DateTime.MinValue;
            if (DateTime.TryParse(rawIssued, out issuedDate))
                this.IssuedDate = issuedDate;

        }

        public List<AuthorInfo> GetAuthorDetails()
        {
            if (string.IsNullOrEmpty(this.RawAuthors))
            {
                return null;
            }

            var result = new List<AuthorInfo>();
            var authorStrings = this.RawAuthors.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var authorItem in authorStrings)
            {
                int countOfSeperators = authorItem.Where(c => c == ',').Count();
                string authorNameString;
                string[] authorNametokens = null;
                string simpleName;
                int birthYear = -1, deathYear = -1;

                if (countOfSeperators <= 1)
                {
                    authorNameString = authorItem;
                    birthYear = -1;
                    deathYear = -1;
                }
                else
                {
                    int lastSeperator = authorItem.LastIndexOf(',');
                    // get the lifespan
                    var dateRangeString = authorItem.Substring(lastSeperator).Trim(new char[] { ' ', ',' });
                    var years = dateRangeString.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    years.TryGetItem(0, out birthYear);
                    years.TryGetItem(1, out deathYear);

                    // get simple author name
                    authorNameString = authorItem.Substring(0, lastSeperator).Trim();
                }

                authorNametokens = authorNameString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                simpleName = string.Join(" ", authorNametokens.Reverse()).Trim();

                result.Add(new AuthorInfo
                {
                    SimpleName = simpleName,
                    LookupName = authorNameString,
                    BirthYear = birthYear,
                    DeathYear = deathYear
                });
            }
            return result;
        }
    }

    public class AuthorInfo
    {
        public string SimpleName { get; set; }

        public string LookupName { get; set; }

        public int BirthYear { get; set; }
        public int DeathYear { get; set; }

        public double LifeSpan()
        {
            if (BirthYear >= 0 && DeathYear >= 0 && DeathYear > BirthYear)
                return this.DeathYear - this.BirthYear;

            return -1;
        }
    }
}
