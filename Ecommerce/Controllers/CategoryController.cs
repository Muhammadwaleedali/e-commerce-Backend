using Ecommerce.Data;
using Ecommerce.Models;
using Ecommerce.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")] 
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(AppDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            _logger.LogInformation("GET /api/Category called");

            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            _logger.LogInformation("Returning {Count} categories", categories.Count);
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            _logger.LogInformation("GET /api/Category/{Id} called", id);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category with Id={Id} not found", id);
                return NotFound();
            }

            return Ok(new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            });
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> PostCategory([FromBody] CategoryDto categoryDto)
        {
            _logger.LogInformation("POST /api/Category called with Name={Name}", categoryDto?.Name);

            if (categoryDto == null || string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                _logger.LogWarning("Invalid category data");
                return BadRequest(new { message = "Category name is required" });
            }

            var category = new Category
            {
                Name = categoryDto.Name
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category created with Id={Id}", category.Id);

            // ✅ return 201 Created with proper route binding
            return CreatedAtAction(
                nameof(GetCategory),       // points to the GET by Id action
                new { id = category.Id },  // route values
                new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name
                });
        }




        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, [FromBody] CategoryDto categoryDto)
        {
            _logger.LogInformation("PUT /api/Category/{Id} called", id);

            if (categoryDto == null || string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                _logger.LogWarning("Invalid category data");
                return BadRequest(new { message = "Category name is required" });
            }

            if (id != categoryDto.Id)
            {
                _logger.LogWarning("Mismatch: URL Id={UrlId}, Body Id={BodyId}", id, categoryDto.Id);
                return BadRequest(new { message = "Id in URL and body do not match" });
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category with Id={Id} not found", id);
                return NotFound();
            }

            category.Name = categoryDto.Name;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category with Id={Id} updated successfully", id);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            _logger.LogInformation("DELETE /api/Category/{Id} called", id);

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                _logger.LogWarning("Category with Id={Id} not found", id);
                return NotFound();
            }

            if (category.Products.Any())
            {
                _logger.LogWarning("Cannot delete category {Id} because it has products", id);
                return BadRequest(new { message = "Cannot delete a category that has products." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category with Id={Id} deleted successfully", id);
            return NoContent();
        }

  
    }
}
