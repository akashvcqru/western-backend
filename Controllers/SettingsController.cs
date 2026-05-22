using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using western_backend.Models;

namespace western_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key.ToLower() == key.ToLower());
            if (setting == null)
            {
                return NotFound(ApiResponse.Error($"Setting with key '{key}' not found"));
            }

            // Parse stored JSON value and wrap in the standard envelope
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(setting.Value);
                return Ok(ApiResponse.Success(parsed, "Setting fetched successfully"));
            }
            catch
            {
                // Fallback: return raw value as string if it can't be deserialized
                return Ok(ApiResponse.Success(setting.Value, "Setting fetched successfully"));
            }
        }

        [HttpPost("{key}")]
        [HttpPut("{key}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save(string key, [FromBody] System.Text.Json.JsonElement value)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key.ToLower() == key.ToLower());

            string jsonString = value.GetRawText();

            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Value = jsonString
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = jsonString;
                _context.Settings.Update(setting);
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Success<object>(null!, $"Setting '{key}' saved successfully"));
        }
    }
}
