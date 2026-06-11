using Souqify.Application.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Interfaces
{
    public interface ICategoryService
    {
        public Task<CategoryDto> AddCategoryAsync(CreateCategoryDto createCategoryDto);
    }
}
