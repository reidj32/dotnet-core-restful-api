using Library.Api.Models;
using System;

namespace Library.Api.Helpers
{
    public class AuthorsResourceParameters
    {
        private const int MaxPageSize = 20;
        private const int DefaultPageSize = 10;
        private const int DefaultPageNumber = 1;
        private const string DefaultOrderBy = nameof(AuthorDto.Name);

        private int _pageSize = DefaultPageSize;

        public int PageNumber { get; set; } = DefaultPageNumber;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Min(value, MaxPageSize);
        }

        public string Genre { get; set; }

        public string SearchQuery { get; set; }

        public string OrderBy { get; set; } = DefaultOrderBy;
    }
}