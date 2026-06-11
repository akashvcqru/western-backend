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
            public string? Location { get; set; } = "Header";
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
                .OrderBy(c => c.Position)
                .ThenBy(c => c.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();
            Console.WriteLine($"[Perf] Categories ToListAsync took {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            var categoryIds = categories.Select(c => c.Id).ToList();
            var categorySlugs = categories.Select(c => c.Slug ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();

            // Fetch counts in a single query grouped by category (case-insensitive key comparison)
            var productCounts = await _context.Products
                .Where(p => categoryIds.Contains(p.Category) || categorySlugs.Contains(p.Category))
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var cat in categories)
            {
                var countFromId = productCounts.GetValueOrDefault(cat.Id, 0);
                var countFromSlug = string.IsNullOrEmpty(cat.Slug) ? 0 : productCounts.GetValueOrDefault(cat.Slug, 0);
                cat.Count = Math.Max(countFromId, countFromSlug);
            }
            Console.WriteLine($"[Perf] Product Count single query took {sw.ElapsedMilliseconds} ms");

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

            // Optimize count query to avoid case translation on the DB column
            category.Count = await _context.Products.CountAsync(p =>
                p.Category == category.Id ||
                p.Category == category.Slug);

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

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Length > 25)
            {
                return BadRequest(ApiResponse.Error("Category Name cannot exceed 25 characters"));
            }

            string slug = GenerateSlug(request.Name);
            string uniqueId = slug;
            int counter = 1;
            while (await _context.Categories.AnyAsync(c => c.Id == uniqueId))
            {
                uniqueId = $"{slug}-{counter++}";
            }

            int maxPosition = 0;
            if (await _context.Categories.AnyAsync())
            {
                maxPosition = await _context.Categories.MaxAsync(c => c.Position);
            }

            var category = new Category
            {
                Id = uniqueId,
                Slug = uniqueId,
                Name = request.Name,
                Description = request.Description,
                Image = FileStorageService.SaveBase64File(request.Image, "category"),
                Status = request.Status,
                Location = request.Location,
                Count = 0,
                Position = maxPosition + 1
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

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Length > 25)
            {
                return BadRequest(ApiResponse.Error("Category Name cannot exceed 25 characters"));
            }

            category.Name = request.Name;
            category.Description = request.Description;
            category.Image = FileStorageService.SaveBase64File(request.Image, "category", category.Image);
            category.Status = request.Status;
            category.Location = request.Location;

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

            FileStorageService.DeleteFile(category.Image);
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
                .OrderBy(c => c.Position)
                .ThenBy(c => c.Name)
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

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Length > 25)
            {
                return BadRequest(ApiResponse.Error("SubCategory Name cannot exceed 25 characters"));
            }

            string slug = GenerateSlug(request.Name);
            string uniqueId = slug;
            int counter = 1;
            while (await _context.SubCategories.AnyAsync(c => c.Id == uniqueId))
            {
                uniqueId = $"{slug}-{counter++}";
            }

            int maxSubPosition = 0;
            if (await _context.SubCategories.AnyAsync())
            {
                maxSubPosition = await _context.SubCategories.MaxAsync(c => c.Position);
            }

            var subCategory = new SubCategory
            {
                Id = uniqueId,
                Slug = uniqueId,
                Name = request.Name,
                Description = request.Description,
                Image = FileStorageService.SaveBase64File(request.Image, "subcategory"),
                CategoryId = request.CategoryId,
                Status = request.Status,
                Position = maxSubPosition + 1
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

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Length > 25)
            {
                return BadRequest(ApiResponse.Error("SubCategory Name cannot exceed 25 characters"));
            }

            subCategory.Name = request.Name;
            subCategory.Description = request.Description;
            subCategory.Image = FileStorageService.SaveBase64File(request.Image, "subcategory", subCategory.Image);
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

            FileStorageService.DeleteFile(subCategory.Image);
            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "SubCategory deleted successfully"));
        }

        public class ReorderItem
        {
            public string Id { get; set; } = string.Empty;
            public int Position { get; set; }
        }

        [HttpPut("reorder")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> ReorderCategories([FromBody] List<ReorderItem> items)
        {
            if (items == null || !items.Any())
            {
                return BadRequest(ApiResponse.Error("Invalid reorder payload"));
            }

            foreach (var item in items)
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == item.Id);
                if (category != null)
                {
                    category.Position = item.Position;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Success<object>(null!, "Categories reordered successfully"));
        }

        [HttpPut("subcategories/reorder")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> ReorderSubCategories([FromBody] List<ReorderItem> items)
        {
            if (items == null || !items.Any())
            {
                return BadRequest(ApiResponse.Error("Invalid reorder payload"));
            }

            foreach (var item in items)
            {
                var subCategory = await _context.SubCategories.FirstOrDefaultAsync(c => c.Id == item.Id);
                if (subCategory != null)
                {
                    subCategory.Position = item.Position;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Success<object>(null!, "Subcategories reordered successfully"));
        }

        private static string GenerateSlug(string name)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(name.ToLower().Trim(), @"[^\w\s-]", "");
            string dashed = cleaned.Replace(" ", "-").Replace("_", "-");
            return System.Text.RegularExpressions.Regex.Replace(dashed, @"-+", "-");
        }
    }
}
