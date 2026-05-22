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
    public class BlogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class BlogRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Excerpt { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Image { get; set; } = string.Empty;
            public string Author { get; set; } = "Admin";
            public string AuthorRole { get; set; } = "Western Interio Admin";
            public List<string> Tags { get; set; } = new();
            public List<string> Content { get; set; } = new();
            public string? Date { get; set; }
            public string? ReadTime { get; set; }
        }

        public BlogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<BlogPost>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            var query = _context.Blogs.AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(b =>
                    b.Title.ToLower().Contains(s) ||
                    b.Excerpt.ToLower().Contains(s) ||
                    b.Category.ToLower().Contains(s) ||
                    b.Author.ToLower().Contains(s));
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                string cat = category.Trim().ToLower();
                query = query.Where(b => b.Category.ToLower() == cat);
            }

            int totalItems = await query.CountAsync();

            var blogs = await query
                .OrderByDescending(b => b.Date)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(blogs, page, totalItems, limit));
        }

        [HttpGet("{idOrSlug}")]
        public async Task<ActionResult<ApiResponse<BlogPost>>> GetById(string idOrSlug)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == idOrSlug);
            if (blog == null)
            {
                return NotFound(ApiResponse.Error($"Blog '{idOrSlug}' not found"));
            }
            return Ok(ApiResponse.Success(blog));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<BlogPost>>> Create([FromBody] BlogRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid blog payload"));
            }

            string slug = GenerateSlug(request.Title);
            string uniqueId = slug;
            int counter = 1;
            while (await _context.Blogs.AnyAsync(b => b.Id == uniqueId))
            {
                uniqueId = $"{slug}-{counter++}";
            }

            var todayStr = DateTime.Now.ToString("MMM dd, yyyy");

            string readTime = request.ReadTime ?? "";
            if (string.IsNullOrEmpty(readTime))
            {
                int wordCount = request.Content.Sum(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                int minutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
                readTime = $"{minutes} Min Read";
            }

            var blog = new BlogPost
            {
                Id = uniqueId,
                Title = request.Title,
                Excerpt = request.Excerpt,
                Category = request.Category,
                Image = request.Image,
                Author = request.Author,
                AuthorRole = request.AuthorRole,
                Tags = request.Tags,
                Content = request.Content,
                Date = request.Date ?? todayStr,
                ReadTime = readTime
            };

            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { idOrSlug = blog.Id },
                ApiResponse.Success(blog, "Blog created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<BlogPost>>> Update(string id, [FromBody] BlogRequest request)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);
            if (blog == null)
            {
                return NotFound(ApiResponse.Error($"Blog '{id}' not found"));
            }

            string readTime = request.ReadTime ?? "";
            if (string.IsNullOrEmpty(readTime))
            {
                int wordCount = request.Content.Sum(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                int minutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
                readTime = $"{minutes} Min Read";
            }

            blog.Title = request.Title;
            blog.Excerpt = request.Excerpt;
            blog.Category = request.Category;
            blog.Image = request.Image;
            blog.Author = request.Author;
            blog.AuthorRole = request.AuthorRole;
            blog.Tags = request.Tags;
            blog.Content = request.Content;
            if (!string.IsNullOrEmpty(request.Date)) blog.Date = request.Date;
            blog.ReadTime = readTime;

            _context.Blogs.Update(blog);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(blog, "Blog updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);
            if (blog == null)
            {
                return NotFound(ApiResponse.Error($"Blog '{id}' not found"));
            }

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Blog deleted successfully"));
        }

        private static string GenerateSlug(string text)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(text.ToLower().Trim(), @"[^\w\s-]", "");
            string dashed = cleaned.Replace(" ", "-").Replace("_", "-");
            return System.Text.RegularExpressions.Regex.Replace(dashed, @"-+", "-");
        }
    }
}
