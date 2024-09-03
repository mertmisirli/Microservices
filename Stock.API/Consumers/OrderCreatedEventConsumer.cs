using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Driver;
using Shared;
using Shared.Events;
using Shared.Messages;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
    {
        IMongoCollection<Entities.Stock> _stockCollection;
        readonly ISendEndpointProvider _sendEndpointProvider;
        IPublishEndpoint _publishEndpoint;
        public OrderCreatedEventConsumer(MongoDBService mongoDBService, ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint)
        {
            _stockCollection = mongoDBService.GetCollection<Entities.Stock>();
            _sendEndpointProvider = sendEndpointProvider;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            List<bool> stockResult = new List<bool>();


            foreach(OrderItemMessage orderItem in context.Message.OrderItems)
            {
                stockResult.Add((await _stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId && s.Count >= orderItem.Count)).Any());
            }

            if(stockResult.TrueForAll(sr => sr.Equals(true)))
            {
                // Geçerli Sipariş

                foreach(OrderItemMessage orderItem in context.Message.OrderItems)
                {
                   Stock.API.Entities.Stock stock = await (await _stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync();

                    stock.Count -= orderItem.Count;

                    await _stockCollection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId, stock);
                }

                // Payment ...
                StockReservedEvent stockReservedEvent = new()
                {
                    BuyerId = context.Message.BuyerId,
                    OrderId = context.Message.OrderId,
                    TotalPrice = context.Message.TotalPrice,
                };

               ISendEndpoint sendEndpoint =  await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.Payment_StockReservedEventQueue}"));

                Console.WriteLine("Stok Uygun");


                await sendEndpoint.Send(stockReservedEvent);
            }
            else
            {
                // Sipariş Geçersiz

                StockNotReservedEvent stockNotReservedEvent = new()
                {
                    BuyerId = context.Message.BuyerId,
                    OrderId = context.Message.OrderId,
                    Message = "Yeterli Stok Bulunamadı"
                };

                Console.WriteLine("Yeterli Stok Yok");
                await _publishEndpoint.Publish(stockNotReservedEvent);
            }

            

        }
    }
}
