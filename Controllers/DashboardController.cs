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
    }
}
