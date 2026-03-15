using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Models
{
    public class PagedList<T>   
    {
        public List<T> Items { get; set; } = new List<T>();

        public int CurrentPage { get;private set; }

        public int TotalPages { get;private set; }

        public int PageSize { get;private set; }

        public int TotalItems { get;private set; }

        public bool HasNext => (CurrentPage < TotalPages);

        public bool HasPrevious => (CurrentPage > 1);

        public PagedList(List<T> items, int currentPage, int pageSize, int totalItems)
        {
            Items = items;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            PageSize = pageSize;
            CurrentPage = currentPage;
        }

        public static PagedList<T> CreatePagination(List<T> items,int pageSize, int totalItems, int currentPage=1)
        {
            var pagedList = new PagedList<T>(items, currentPage, pageSize, totalItems);

            return pagedList;
        }

    }
}
