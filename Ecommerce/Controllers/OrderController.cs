using Ecommerce.Data;
using Ecommerce.Models;
using Ecommerce.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        private static OrderResponseDto MapToDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                Items = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    Id = oi.Id,
                    Quantity = oi.Quantity,
                    Product = new ProductDto
                    {
                        Id = oi.Product.Id,
                        Name = oi.Product.Name,
                        Price = oi.Product.Price,
                        Description = oi.Product.Description,
                        CategoryId = oi.Product.CategoryId,
                        CategoryName = oi.Product.Category?.Name,
                        ImageUrl = oi.Product.ImageUrl
                    }
                }).ToList()
            };
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            if (orderDto.Items == null || !orderDto.Items.Any())
                return BadRequest("Order must contain at least one product.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? orderDto.UserId;

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                OrderItems = new List<OrderItems>()
            };

            decimal total = 0;

            foreach (var item in orderDto.Items)
            {
                var product = await _context.Products
                    .Include(p => p.Category) 
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                    return NotFound($"Product with ID {item.ProductId} not found.");

                if (product.Stock < item.Quantity)
                    return BadRequest($"Not enough stock for product {product.Name}.");

                var orderItem = new OrderItems
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price,
                    Product = product
                };

                product.Stock -= item.Quantity;
                total += orderItem.Price * orderItem.Quantity;
                order.OrderItems.Add(orderItem);
            }

            order.TotalAmount = total;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(order));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return Ok(MapToDto(order));
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .ToListAsync();

            return Ok(orders.Select(MapToDto).ToList());
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateMyOrder(int id, [FromBody] CreateOrderDto orderDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound("Order not found or not yours.");
            if (order.Status != "Pending")
                return BadRequest("You can only update orders that are still pending.");

            _context.OrderItems.RemoveRange(order.OrderItems);
            order.OrderItems.Clear();

            decimal total = 0;
            foreach (var item in orderDto.Items)
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null) return NotFound($"Product with ID {item.ProductId} not found.");
                if (product.Stock < item.Quantity) return BadRequest($"Not enough stock for {product.Name}.");

                var orderItem = new OrderItems
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price,
                    Product = product
                };

                total += orderItem.Price * orderItem.Quantity;
                order.OrderItems.Add(orderItem);
            }

            order.TotalAmount = total;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order updated successfully", OrderId = order.Id });
        }

        [HttpDelete("cancel/{id}")]
        public async Task<IActionResult> CancelMyOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound("Order not found or not yours.");
            if (order.Status != "Pending") return BadRequest("Only pending orders can be canceled.");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order canceled successfully", OrderId = id });
        }
    }
}
