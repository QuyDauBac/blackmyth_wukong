using System;
using System.Collections.Generic;
using System.Linq;

namespace WukongWeb.Infrastructure
{
    public class PagedResult<T>
    {
        public PagedResult()
        {
            Items = new List<T>();
        }

        public IList<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages
        {
            get { return PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize); }
        }

        public bool HasPrevious
        {
            get { return Page > 1; }
        }

        public bool HasNext
        {
            get { return Page < TotalPages; }
        }
    }
}
