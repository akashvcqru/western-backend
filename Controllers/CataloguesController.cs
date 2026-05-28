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
    public class CataloguesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class CatalogueRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
            public string PdfData { get; set; } = string.Empty;
            public string PdfFileName { get; set; } = string.Empty;
            public string Status { get; set; } = "Active";
        }

        /// <summary>
        /// Lightweight DTO returned in list responses — excludes the heavy PdfData field.
        /// </summary>
        public class CatalogueListItem
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
            public string PdfFileName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public System.DateTime CreatedAt { get; set; }
            public bool HasPdf { get; set; }
        }

        public CataloguesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/catalogues — Public paginated list (without PdfData to keep responses small).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<CatalogueListItem>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 12,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? status = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 12;
            if (limit > 100) limit = 100;

            var query = _context.Catalogues.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(s) ||
                    c.Category.ToLower().Contains(s) ||
                    c.Description.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                string cat = category.Trim().ToLower();
                query = query.Where(c => c.Category.ToLower() == cat);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                string st = status.Trim().ToLower();
                query = query.Where(c => c.Status.ToLower() == st);
            }

            int totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new CatalogueListItem
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Category = c.Category,
                    Image = c.Image,
                    PdfFileName = c.PdfFileName,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    HasPdf = !string.IsNullOrEmpty(c.PdfData)
                })
                .ToListAsync();

            return Ok(ApiResponse.Paginated(items, page, totalItems, limit));
        }

        /// <summary>
        /// GET /api/catalogues/{id} — Single item with full PdfData for download.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Catalogue>>> GetById(string id)
        {
            var item = await _context.Catalogues.FindAsync(id);
            if (item == null)
            {
                return NotFound(ApiResponse.Error($"Catalogue '{id}' not found"));
            }
            return Ok(ApiResponse.Success(item));
        }

        /// <summary>
        /// POST /api/catalogues — Admin-only create.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Catalogue>>> Create([FromBody] CatalogueRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid catalogue payload"));
            }

            var item = new Catalogue
            {
                Id = System.Guid.NewGuid().ToString(),
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Image = FileStorageService.SaveBase64File(request.Image, "catalogue"),
                PdfData = FileStorageService.SaveBase64File(request.PdfData, "catalogue/pdfs"),
                PdfFileName = request.PdfFileName,
                Status = request.Status,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.Catalogues.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                ApiResponse.Success(item, "Catalogue created successfully"));
        }

        /// <summary>
        /// PUT /api/catalogues/{id} — Admin-only update.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Catalogue>>> Update(string id, [FromBody] CatalogueRequest request)
        {
            var item = await _context.Catalogues.FindAsync(id);
            if (item == null)
            {
                return NotFound(ApiResponse.Error($"Catalogue '{id}' not found"));
            }

            item.Title = request.Title;
            item.Description = request.Description;
            item.Category = request.Category;
            item.Image = FileStorageService.SaveBase64File(request.Image, "catalogue", item.Image);
            item.Status = request.Status;

            // Only update PDF if a new one is provided
            if (!string.IsNullOrEmpty(request.PdfData))
            {
                item.PdfData = FileStorageService.SaveBase64File(request.PdfData, "catalogue/pdfs", item.PdfData);
                item.PdfFileName = request.PdfFileName;
            }

            _context.Catalogues.Update(item);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(item, "Catalogue updated successfully"));
        }

        /// <summary>
        /// DELETE /api/catalogues/{id} — Admin-only delete.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var item = await _context.Catalogues.FindAsync(id);
            if (item == null)
            {
                return NotFound(ApiResponse.Error($"Catalogue '{id}' not found"));
            }

            FileStorageService.DeleteFile(item.Image);
            FileStorageService.DeleteFile(item.PdfData);
            _context.Catalogues.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Catalogue deleted successfully"));
        }
    }
}
