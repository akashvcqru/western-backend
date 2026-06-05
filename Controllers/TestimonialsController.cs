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
    public class TestimonialsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class TestimonialSubmitRequest
        {
            public string Author { get; set; } = string.Empty;
            public string Designation { get; set; } = string.Empty;
            public string Company { get; set; } = string.Empty;
            public string Quote { get; set; } = string.Empty;
            public int Rating { get; set; } = 5;
            public string Category { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
        }

        public class TestimonialUpdateRequest
        {
            public string Author { get; set; } = string.Empty;
            public string Designation { get; set; } = string.Empty;
            public string Company { get; set; } = string.Empty;
            public string Quote { get; set; } = string.Empty;
            public int Rating { get; set; } = 5;
            public string Category { get; set; } = string.Empty;
            public string Status { get; set; } = "Active";
            public string Image { get; set; } = string.Empty;
        }

        public TestimonialsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/testimonials (Public - Active Only)
        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<Testimonial>>> GetActive(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 100,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 100;
            if (limit > 100) limit = 100;

            var query = _context.Testimonials
                .Where(t => t.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
            {
                query = query.Where(t => t.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(t =>
                    t.Author.ToLower().Contains(s) ||
                    t.Quote.ToLower().Contains(s) ||
                    t.Company.ToLower().Contains(s));
            }

            int totalItems = await query.CountAsync();

            var testimonials = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(testimonials, page, totalItems, limit));
        }

        // GET: api/testimonials/admin (Admin Only - All Testimonials)
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaginatedApiResponse<Testimonial>>> GetAdminAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 50;
            if (limit > 100) limit = 100;

            var query = _context.Testimonials.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(t =>
                    t.Author.ToLower().Contains(s) ||
                    t.Quote.ToLower().Contains(s) ||
                    t.Company.ToLower().Contains(s));
            }

            int totalItems = await query.CountAsync();

            var testimonials = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(testimonials, page, totalItems, limit));
        }

        // POST: api/testimonials (Public - Client review submission)
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Testimonial>>> Create([FromBody] TestimonialSubmitRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid review payload"));
            }

            var testimonial = new Testimonial
            {
                Id = Guid.NewGuid().ToString(),
                Author = request.Author,
                Designation = request.Designation,
                Company = request.Company,
                Quote = request.Quote,
                Rating = request.Rating,
                Category = request.Category,
                Status = "Pending", // Sent to admin panel for review
                CreatedAt = DateTime.UtcNow,
                Image = string.IsNullOrEmpty(request.Image) ? string.Empty : FileStorageService.SaveBase64File(request.Image, "testimonial")
            };

            _context.Testimonials.Add(testimonial);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(testimonial, "Review submitted successfully"));
        }

        // POST: api/testimonials/admin (Admin Only - Add testimonial)
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Testimonial>>> CreateAdmin([FromBody] TestimonialUpdateRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid testimonial payload"));
            }

            var testimonial = new Testimonial
            {
                Id = Guid.NewGuid().ToString(),
                Author = request.Author,
                Designation = request.Designation,
                Company = request.Company,
                Quote = request.Quote,
                Rating = request.Rating,
                Category = request.Category,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
                CreatedAt = DateTime.UtcNow,
                Image = string.IsNullOrEmpty(request.Image) ? string.Empty : FileStorageService.SaveBase64File(request.Image, "testimonial")
            };

            _context.Testimonials.Add(testimonial);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(testimonial, "Testimonial created successfully by admin"));
        }

        // PUT: api/testimonials/{id} (Admin Only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Testimonial>>> Update(string id, [FromBody] TestimonialUpdateRequest request)
        {
            var testimonial = await _context.Testimonials.FirstOrDefaultAsync(t => t.Id == id);
            if (testimonial == null)
            {
                return NotFound(ApiResponse.Error($"Testimonial with ID '{id}' not found"));
            }

            // Differentiate between full update (from edit form) and partial status toggle
            if (!string.IsNullOrEmpty(request.Author))
            {
                testimonial.Author = request.Author;
                testimonial.Designation = request.Designation ?? string.Empty;
                testimonial.Company = request.Company ?? string.Empty;
                testimonial.Quote = request.Quote;
                if (request.Rating > 0)
                    testimonial.Rating = request.Rating;
                if (!string.IsNullOrEmpty(request.Category))
                    testimonial.Category = request.Category;

                // Handle image update/delete
                if (request.Image != testimonial.Image)
                {
                    if (string.IsNullOrEmpty(request.Image))
                    {
                        FileStorageService.DeleteFile(testimonial.Image);
                        testimonial.Image = string.Empty;
                    }
                    else
                    {
                        testimonial.Image = FileStorageService.SaveBase64File(request.Image, "testimonial", testimonial.Image);
                    }
                }
            }

            if (!string.IsNullOrEmpty(request.Status))
                testimonial.Status = request.Status;

            _context.Testimonials.Update(testimonial);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(testimonial, "Testimonial updated successfully"));
        }

        // DELETE: api/testimonials/{id} (Admin Only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var testimonial = await _context.Testimonials.FirstOrDefaultAsync(t => t.Id == id);
            if (testimonial == null)
            {
                return NotFound(ApiResponse.Error($"Testimonial with ID '{id}' not found"));
            }

            // Delete associated physical image if exists
            if (!string.IsNullOrEmpty(testimonial.Image))
            {
                FileStorageService.DeleteFile(testimonial.Image);
            }

            _context.Testimonials.Remove(testimonial);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Testimonial deleted successfully"));
        }
    }
}
