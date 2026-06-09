using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace western_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public class DashboardStats
        {
            public int Products { get; set; }
            public int Categories { get; set; }
            public int Brands { get; set; }
            public int Gallery { get; set; }
            public int Blogs { get; set; }
            public int TotalInquiries { get; set; }
            public int PendingInquiries { get; set; }
        }

        public class ActivityItem
        {
            public string Id { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Detail { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<DashboardStats>>> GetStats()
        {
            var stats = new DashboardStats
            {
                Products = await _context.Products.CountAsync(),
                Categories = await _context.Categories.CountAsync(),
                Brands = await _context.Brands.CountAsync(),
                Gallery = await _context.Gallery.CountAsync(),
                Blogs = await _context.Blogs.CountAsync(),
                TotalInquiries = await _context.Inquiries.CountAsync(),
                PendingInquiries = await _context.Inquiries.CountAsync(i => i.Status.ToLower() == "new")
            };

            return Ok(ApiResponse.Success(stats, "Dashboard stats fetched successfully"));
        }

        [HttpGet("recent-activity")]
        public async Task<ActionResult<ApiResponse<List<ActivityItem>>>> GetRecentActivity([FromQuery] int limit = 10)
        {
            var activities = new List<ActivityItem>();

            // ── Inquiries (most important real-time data) ──
            var recentInquiries = await _context.Inquiries
                .OrderByDescending(i => i.Id)
                .Take(limit)
                .ToListAsync();

            foreach (var inq in recentInquiries)
            {
                var status = (inq.Status ?? "new").ToLower();
                var action = status switch
                {
                    "new" => "New inquiry received",
                    "resolved" => "Inquiry resolved",
                    "in-progress" or "in progress" => "Inquiry in progress",
                    _ => "Inquiry updated"
                };

                activities.Add(new ActivityItem
                {
                    Id = $"inquiry-{inq.Id}",
                    Action = action,
                    Detail = $"From {inq.Name} — {inq.Subject}",
                    Type = "inquiry",
                    Timestamp = ParseDateString(inq.Date)
                });
            }

            // ── Blogs ──
            var recentBlogs = await _context.Blogs
                .OrderByDescending(b => b.Date)
                .Take(5)
                .ToListAsync();

            foreach (var blog in recentBlogs)
            {
                activities.Add(new ActivityItem
                {
                    Id = $"blog-{blog.Id}",
                    Action = "Blog post published",
                    Detail = blog.Title,
                    Type = "blog",
                    Timestamp = ParseDateString(blog.Date)
                });
            }

            // ── Testimonials ──
            var recentTestimonials = await _context.Testimonials
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var test in recentTestimonials)
            {
                activities.Add(new ActivityItem
                {
                    Id = $"testimonial-{test.Id}",
                    Action = "Testimonial added",
                    Detail = $"{test.Author} — {test.Company}",
                    Type = "testimonial",
                    Timestamp = test.CreatedAt
                });
            }

            // ── Catalogues ──
            var recentCatalogues = await _context.Catalogues
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var cat in recentCatalogues)
            {
                activities.Add(new ActivityItem
                {
                    Id = $"catalogue-{cat.Id}",
                    Action = "Catalogue added",
                    Detail = cat.Title,
                    Type = "catalogue",
                    Timestamp = cat.CreatedAt
                });
            }

            // ── Services ──
            var recentServices = await _context.Services
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var svc in recentServices)
            {
                activities.Add(new ActivityItem
                {
                    Id = $"service-{svc.Id}",
                    Action = "Service added",
                    Detail = svc.Title,
                    Type = "service",
                    Timestamp = svc.CreatedAt
                });
            }

            // Sort all activities by timestamp descending and take the requested limit
            var result = activities
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();

            return Ok(ApiResponse.Success(result, "Recent activity fetched successfully"));
        }

        /// <summary>
        /// Best-effort parse of the Date string fields used by Inquiry / BlogPost.
        /// Falls back to DateTime.MinValue so the item still appears in the list.
        /// </summary>
        private static DateTime ParseDateString(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return DateTime.MinValue;

            // Try standard parseable formats first
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

            // Try common display formats: "15 Jan 2025", "January 15, 2025", etc.
            string[] formats = {
                "dd MMM yyyy", "d MMM yyyy", "MMM dd, yyyy", "MMMM dd, yyyy",
                "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy"
            };

            if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;

            return DateTime.MinValue;
        }
    }
}

