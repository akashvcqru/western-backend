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
    public class InquiriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class CreateInquiryRequest
        {
            public string? Name { get; set; }
            public string? FullName { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Subject { get; set; }
            public string? Message { get; set; }
        }

        public InquiriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaginatedApiResponse<Inquiry>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            var query = _context.Inquiries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(s) ||
                    i.Email.ToLower().Contains(s) ||
                    i.Subject.ToLower().Contains(s) ||
                    (i.Phone != null && i.Phone.Contains(s)) ||
                    (i.Message != null && i.Message.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
            {
                string st = status.Trim().ToLower();
                query = query.Where(i => i.Status.ToLower() == st);
            }

            int totalItems = await query.CountAsync();

            var inquiries = await query
                .OrderByDescending(i => i.Id)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(inquiries, page, totalItems, limit));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Inquiry>>> Create([FromBody] CreateInquiryRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid inquiry payload"));
            }

            string customerName = request.FullName ?? request.Name ?? "Anonymous";
            string subject = request.Subject ?? "General Inquiry";
            string message = request.Message ?? "No message specified.";
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd");

            long timestampId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            while (await _context.Inquiries.AnyAsync(i => i.Id == timestampId))
            {
                timestampId++;
            }

            var inquiry = new Inquiry
            {
                Id = timestampId,
                Name = customerName,
                Email = request.Email,
                Phone = request.Phone,
                Subject = subject,
                Message = message,
                Date = dateStr,
                Status = "new"
            };

            _context.Inquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(inquiry, "Inquiry submitted successfully"));
        }

        [HttpPut("{id}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Inquiry>>> Resolve(long id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry == null)
            {
                return NotFound(ApiResponse.Error("Inquiry not found"));
            }

            inquiry.Status = "resolved";
            _context.Inquiries.Update(inquiry);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(inquiry, "Inquiry resolved successfully"));
        }
    }
}
