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
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<Product>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? brand = null,
            [FromQuery] string? status = null)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            var query = _context.Products.AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(s) ||
                    p.Brand.ToLower().Contains(s) ||
                    p.Id.ToLower().Contains(s) ||
                    p.Slug.ToLower().Contains(s) ||
                    p.Category.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(p => p.Brand.ToLower() == brand.ToLower());

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status.ToLower() == status.ToLower());

            int totalItems = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse.Paginated(products, page, totalItems, limit));
        }

        [HttpGet("{idOrSlug}")]
        public async Task<ActionResult<ApiResponse<Product>>> GetById(string idOrSlug)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p =>
                p.Id.ToLower() == idOrSlug.ToLower() ||
                p.Slug.ToLower() == idOrSlug.ToLower());

            if (product == null)
            {
                return NotFound(ApiResponse.Error($"Product '{idOrSlug}' not found"));
            }

            return Ok(ApiResponse.Success(product));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Product>>> Create([FromBody] Product product)
        {
            if (product == null)
            {
                return BadRequest(ApiResponse.Error("Invalid product payload"));
            }

            string slug = string.IsNullOrEmpty(product.Slug) ? GenerateSlug(product.Name) : product.Slug;
            string uniqueId = string.IsNullOrEmpty(product.Id) ? slug : product.Id;

            int counter = 1;
            while (await _context.Products.AnyAsync(p => p.Id == uniqueId || p.Slug == uniqueId))
            {
                uniqueId = $"{slug}-{counter++}";
            }

            product.Id = uniqueId;
            product.Slug = uniqueId;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { idOrSlug = product.Id },
                ApiResponse.Success(product, "Product created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Product>>> Update(string id, [FromBody] Product updatedProduct)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound(ApiResponse.Error($"Product with ID '{id}' not found"));
            }

            product.Name = updatedProduct.Name;
            product.Category = updatedProduct.Category;
            product.Brand = updatedProduct.Brand;
            product.Price = updatedProduct.Price;
            product.Status = updatedProduct.Status;
            product.Stock = updatedProduct.Stock;
            product.Description = updatedProduct.Description;
            product.Images = updatedProduct.Images;
            product.Image = updatedProduct.Image;
            product.CatNo = updatedProduct.CatNo;
            product.BlueprintImage = updatedProduct.BlueprintImage;
            product.Material = updatedProduct.Material;
            product.Finish = updatedProduct.Finish;
            product.Size = updatedProduct.Size;
            product.Features = updatedProduct.Features;
            product.Specifications = updatedProduct.Specifications;
            product.Dimensions = updatedProduct.Dimensions;
            product.Resources = updatedProduct.Resources;
            product.Variants = updatedProduct.Variants;
            product.Swatches = updatedProduct.Swatches;
            product.DetailsTitle = updatedProduct.DetailsTitle;
            product.DetailsText1 = updatedProduct.DetailsText1;
            product.DetailsText2 = updatedProduct.DetailsText2;
            product.QuickSpecs = updatedProduct.QuickSpecs;

            if (!string.IsNullOrEmpty(updatedProduct.Slug))
                product.Slug = updatedProduct.Slug;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success(product, "Product updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound(ApiResponse.Error($"Product with ID '{id}' not found"));
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object>(null!, "Product deleted successfully"));
        }

        private static string GenerateSlug(string text)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(text.ToLower().Trim(), @"[^\w\s-]", "");
            string dashed = cleaned.Replace(" ", "-").Replace("_", "-");
            return System.Text.RegularExpressions.Regex.Replace(dashed, @"-+", "-");
        }
    }
}
