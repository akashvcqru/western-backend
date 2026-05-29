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
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class ServiceRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? Category { get; set; }
            public string? Icon { get; set; }
            public string Status { get; set; } = "Active";
        }

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/services (Public - Active Only)
        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<Service>>> GetActive(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 100,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 100;
            if (limit > 100) limit = 100;

            var query = _context.Services
                .Where(s => s.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all" && category.ToLower() != "all services")
            {
                query = query.Where(s => s.Category != null && s.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(service =>
                    service.Title.ToLower().Contains(s) ||
                    (service.Description != null && service.Description.ToLower().Contains(s)));
            }

            int totalItems = await query.CountAsync();

            var services = await query
                .OrderBy(s => s.Title)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(services, page, totalItems, limit));
        }

        // GET: api/services/admin (Admin Only - All Services)
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaginatedApiResponse<Service>>> GetAdminAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 50;
            if (limit > 100) limit = 100;

            var query = _context.Services.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(service =>
                    service.Title.ToLower().Contains(s) ||
                    (service.Description != null && service.Description.ToLower().Contains(s)));
            }

            int totalItems = await query.CountAsync();

            var services = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(services, page, totalItems, limit));
        }

        // GET: api/services/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Service>>> GetById(string id)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (service == null)
            {
                return NotFound(ApiResponse.Error($"Service with ID '{id}' not found"));
            }

            return Ok(ApiResponse.Success(service));
        }

        // POST: api/services/admin (Admin Only)
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Service>>> Create([FromBody] ServiceRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid service payload"));
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(ApiResponse.Error("Service name/title is required"));
            }

            // Slugify title
            string slug = request.Title.ToLower()
                .Replace(" ", "-")
                .Replace("&", "and")
                .Replace("/", "-")
                .Trim();
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

            var service = new Service
            {
                Id = Guid.NewGuid().ToString(),
                Slug = slug,
                Title = request.Title,
                Description = request.Description ?? "",
                Category = string.IsNullOrWhiteSpace(request.Category) ? "partitions" : request.Category,
                Icon = string.IsNullOrWhiteSpace(request.Icon) ? "Sparkles" : request.Icon,
                Image = FileStorageService.SaveBase64File(request.Image, "service"),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = service.Id },
                ApiResponse.Success(service, "Service created successfully by admin"));
        }

        // PUT: api/services/{id} (Admin Only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Service>>> Update(string id, [FromBody] ServiceRequest request)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (service == null)
            {
                return NotFound(ApiResponse.Error($"Service with ID '{id}' not found"));
            }

            if (!string.IsNullOrEmpty(request.Title))
            {
                service.Title = request.Title;
                // Update slug if name changes
                string slug = request.Title.ToLower()
                    .Replace(" ", "-")
                    .Replace("&", "and")
                    .Replace("/", "-")
                    .Trim();
                slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
                service.Slug = slug;
            }

            if (request.Description != null)
                service.Description = request.Description;

            if (request.Category != null)
                service.Category = request.Category;

            if (request.Icon != null)
                service.Icon = request.Icon;

            if (!string.IsNullOrEmpty(request.Image))
            {
                service.Image = FileStorageService.SaveBase64File(request.Image, "service", service.Image);
            }

            if (!string.IsNullOrEmpty(request.Status))
                service.Status = request.Status;

            _context.Services.Update(service);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(service, "Service updated successfully"));
        }

        // DELETE: api/services/{id} (Admin Only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (service == null)
            {
                return NotFound(ApiResponse.Error($"Service with ID '{id}' not found"));
            }

            try
            {
                FileStorageService.DeleteFile(service.Image);
            }
            catch { /* Ignored if file deletion fails */ }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Service deleted successfully"));
        }
    }
}
