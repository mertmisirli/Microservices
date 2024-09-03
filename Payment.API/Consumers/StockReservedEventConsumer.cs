using MassTransit;
using Shared.Events;

namespace Payment.API.Consumers
{
    public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
    {
        IPublishEndpoint _publishEndpoint;

        public StockReservedEventConsumer(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<StockReservedEvent> context)
        {
            if(true)
            {
                // Ödeme Başarılı ...

                PaymentCompletedEvent paymentCompletedEvent = new()
                {
                    OrderId = context.Message.OrderId
                };

                await _publishEndpoint.Publish(paymentCompletedEvent);
                Console.WriteLine("Ödeme Başarılı ...");

            }
            else
            {
                // Ödeme Başarısız ...

                PaymentFailedEvent paymentFailedEvent = new()
                {
                    OrderId = context.Message.OrderId
                };

                await _publishEndpoint.Publish(paymentFailedEvent);

                Console.WriteLine("Ödeme Başarısız ...");
            }

        }
    }
}
