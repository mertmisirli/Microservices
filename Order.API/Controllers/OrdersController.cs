using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.API.Models;
using Order.API.Models.Entities;
using Order.API.ViewModels;
using Shared.Events;

namespace Order.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        readonly OrderAPIDbContext _context;
        readonly IPublishEndpoint _publishEndpoint;

        public OrdersController(OrderAPIDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderVM createOrderVM)
        {
            Models.Entities.Order order = new()
            {
                OrderId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                OrderStatu = Models.Enums.OrderStatus.Suspended,

            };
            order.OrderItems = createOrderVM.OrderItems.Select(oi => new OrderItem
            {
                ProductId = oi.ProductId,
                Count = oi.Count,
                Price = oi.Price,
            }).ToList();

            order.TotalPrice = createOrderVM.OrderItems.Sum(oi => (oi.Price * oi.Count));

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            OrderCreatedEvent orderCreatedEvent = new()
            {
                BuyerId = order.BuyerId,
                OrderId = order.OrderId,
                OrderItems = order.OrderItems.Select(oi => new Shared.Messages.OrderItemMessage
                {
                    ProductId = oi.ProductId.ToString(),
                    Count = oi.Count,
                }).ToList(),
                TotalPrice = order.TotalPrice,
            };

            await _publishEndpoint.Publish(orderCreatedEvent);

            return Ok();
        }
    }
}
