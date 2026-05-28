using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using western_backend.Models;

namespace western_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class CategoryRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
            public string Status { get; set; } = "Active";
        }

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<Category>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(s) ||
                    c.Id.ToLower().Contains(s) ||
                    c.Description.ToLower().Contains(s));
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int totalItems = await query.CountAsync();
            Console.WriteLine($"[Perf] Categories CountAsync took {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            var categories = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();
            Console.WriteLine($"[Perf] Categories ToListAsync took {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            // Calculate product counts dynamically using database count queries
            foreach (var cat in categories)
            {
                var catId = cat.Id.ToLower();
                var catSlug = (cat.Slug ?? "").ToLower();
                cat.Count = await _context.Products.CountAsync(p =>
                    p.Category.ToLower() == catId ||
                    p.Category.ToLower() == catSlug);
            }
            Console.WriteLine($"[Perf] Product Count loops took {sw.ElapsedMilliseconds} ms");

            return Ok(ApiResponse.Paginated(categories, page, totalItems, limit));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Category>>> GetById(string id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id || c.Slug == id);
            if (category == null)
            {
                return NotFound(ApiResponse.Error($"Category '{id}' not found"));
            }

            category.Count = await _context.Products.CountAsync(p =>
                p.Category.ToLower() == category.Id.ToLower() ||
                p.Category.ToLower() == (category.Slug ?? "").ToLower());

            return Ok(ApiResponse.Success(category));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Category>>> Create([FromBody] CategoryRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid category payload"));
            }

            string slug = GenerateSlug(request.Name);
            string uniqueId = slug;
            int counter = 1;
            while (await _context.Categories.AnyAsync(c => c.Id == uniqueId))
            {
                uniqueId = $"{slug}-{counter++}";
            }

            var category = new Category
            {
                Id = uniqueId,
                Slug = uniqueId,
                Name = request.Name,
                Description = request.Description,
                Image = request.Image,
                Status = request.Status,
                Count = 0
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id },
                ApiResponse.Success(category, "Category created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Category>>> Update(string id, [FromBody] CategoryRequest request)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound(ApiResponse.Error($"Category '{id}' not found"));
            }

            category.Name = request.Name;
            category.Description = request.Description;
            category.Image = request.Image;
            category.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(category, "Category updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound(ApiResponse.Error($"Category '{id}' not found"));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Category deleted successfully"));
        }

        public class SubCategoryRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
            public string CategoryId { get; set; } = string.Empty;
            public string Status { get; set; } = "Active";
        }

        [HttpGet("subcategories")]
        public async Task<ActionResult<PaginatedApiResponse<SubCategory>>> GetAllSubcategories(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 100,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 100;
            if (limit > 100) limit = 100;

            var query = _context.SubCategories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(s) ||
                    c.Id.ToLower().Contains(s) ||
                    c.Description.ToLower().Contains(s));
            }

            int totalItems = await query.CountAsync();

            var subCategories = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(subCategories, page, totalItems, limit));
        }

        [HttpGet("subcategories/{id}")]
        public async Task<ActionResult<ApiResponse<SubCategory>>> GetSubcategoryById(string id)
        {
            var subCategory = await _context.SubCategories.FirstOrDefaultAsync(c => c.Id == id || c.Slug == id);
            if (subCategory == null)
            {
                return NotFound(ApiResponse.Error($"SubCategory '{id}' not found"));
            }
            return Ok(ApiResponse.Success(subCategory));
        }

        [HttpPost("subcategories")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SubCategory>>> CreateSubcategory([FromBody] SubCategoryRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid subcategory payload"));
            }

            string slug = GenerateSlug(request.Name);
            string uniqueId = slug;
            int counter = 1;
            while (await _context.SubCategories.AnyAsync(c => c.Id == uniqueId))
            {
                uniqueId = $"{slug}-{counter++}";
            }

            var subCategory = new SubCategory
            {
                Id = uniqueId,
                Slug = uniqueId,
                Name = request.Name,
                Description = request.Description,
                Image = request.Image,
                CategoryId = request.CategoryId,
                Status = request.Status
            };

            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubcategoryById), new { id = subCategory.Id },
                ApiResponse.Success(subCategory, "SubCategory created successfully"));
        }

        [HttpPut("subcategories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SubCategory>>> UpdateSubcategory(string id, [FromBody] SubCategoryRequest request)
        {
            var subCategory = await _context.SubCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (subCategory == null)
            {
                return NotFound(ApiResponse.Error($"SubCategory '{id}' not found"));
            }

            subCategory.Name = request.Name;
            subCategory.Description = request.Description;
            subCategory.Image = request.Image;
            subCategory.CategoryId = request.CategoryId;
            subCategory.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(subCategory, "SubCategory updated successfully"));
        }

        [HttpDelete("subcategories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSubcategory(string id)
        {
            var subCategory = await _context.SubCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (subCategory == null)
            {
                return NotFound(ApiResponse.Error($"SubCategory '{id}' not found"));
            }

            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "SubCategory deleted successfully"));
        }

        private static string GenerateSlug(string name)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(name.ToLower().Trim(), @"[^\w\s-]", "");
            string dashed = cleaned.Replace(" ", "-").Replace("_", "-");
            return System.Text.RegularExpressions.Regex.Replace(dashed, @"-+", "-");
        }
    }
}
