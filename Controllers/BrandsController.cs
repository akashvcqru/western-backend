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
    public class BrandsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class BrandRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string Link { get; set; } = string.Empty;
        }

        public BrandsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<Brand>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            var query = _context.Brands.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(b =>
                    b.Name.ToLower().Contains(s) ||
                    b.Id.ToLower().Contains(s));
            }

            int totalItems = await query.CountAsync();

            var brands = await query
                .OrderBy(b => b.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(brands, page, totalItems, limit));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Brand>>> GetById(string id)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b =>
                b.Id == id || b.Name.ToLower() == id.ToLower());

            if (brand == null)
            {
                return NotFound(ApiResponse.Error($"Brand '{id}' not found"));
            }

            return Ok(ApiResponse.Success(brand));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Brand>>> Create([FromBody] BrandRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Error("Invalid brand payload"));
            }

            var brand = new Brand
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Url = FileStorageService.SaveBase64File(request.Url, "brand"),
                Link = request.Link
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = brand.Id },
                ApiResponse.Success(brand, "Brand created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Brand>>> Update(string id, [FromBody] BrandRequest request)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound(ApiResponse.Error($"Brand '{id}' not found"));
            }

            brand.Name = request.Name;
            brand.Url = FileStorageService.SaveBase64File(request.Url, "brand", brand.Url);
            brand.Link = request.Link;

            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(brand, "Brand updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound(ApiResponse.Error($"Brand '{id}' not found"));
            }

            FileStorageService.DeleteFile(brand.Url);
            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Brand deleted successfully"));
        }
    }
}
