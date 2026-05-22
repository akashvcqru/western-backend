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
    public class GalleryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class GalleryRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
        }

        public GalleryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<GalleryItem>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 12,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 12;
            if (limit > 100) limit = 100;

            var query = _context.Gallery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(g =>
                    g.Title.ToLower().Contains(s) ||
                    g.Category.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                string cat = category.Trim().ToLower();
                query = query.Where(g => g.Category.ToLower() == cat);
            }

            int totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(g => g.Id)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(items, page, totalItems, limit));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<GalleryItem>>> GetById(int id)
        {
            var item = await _context.Gallery.FindAsync(id);
            if (item == null)
            {
                return NotFound(ApiResponse.Error($"Gallery item '{id}' not found"));
            }
            return Ok(ApiResponse.Success(item));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<GalleryItem>>> Create([FromBody] GalleryRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid gallery payload"));
            }

            var item = new GalleryItem
            {
                Title = request.Title,
                Category = request.Category,
                Image = request.Image
            };

            _context.Gallery.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                ApiResponse.Success(item, "Gallery item created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<GalleryItem>>> Update(int id, [FromBody] GalleryRequest request)
        {
            var item = await _context.Gallery.FindAsync(id);
            if (item == null)
            {
                return NotFound(ApiResponse.Error($"Gallery item '{id}' not found"));
            }

            item.Title = request.Title;
            item.Category = request.Category;
            item.Image = request.Image;

            _context.Gallery.Update(item);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(item, "Gallery item updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Gallery.FindAsync(id);
            if (item == null)
            {
                return NotFound(ApiResponse.Error($"Gallery item '{id}' not found"));
            }

            _context.Gallery.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Gallery item deleted successfully"));
        }
    }
}
