using System;
using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using NServiceBus;
using NServiceBus.Config;
using OrderService.Messages;

namespace Partner
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Partner Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                NServiceBus.Config.Configure.With(builder)
                    .InterfaceToXMLSerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .SetMessageHandlersFromAssembliesInOrder(
                            typeof(OrderStatusChangedMessageHandler).Assembly
                        );

                IBus bus = builder.Build<IBus>();
                bus.Start();

                Guid partnerId = Guid.NewGuid();
                Guid productId = Guid.NewGuid();
                float quantity = 10.0F;
                List<OrderLine> orderlines;

                Console.WriteLine("Enter the quantity you wish to order.\nSignal a complete PO with 'y'.\nTo exit, enter 'q'.");
                string line;
                string poId = Guid.NewGuid().ToString();
                while ((line = Console.ReadLine().ToLower()) != "q")
                {
                    if (line == "simulate")
                        Simulate(bus);

                    bool done = (line == "y");
                    orderlines = new List<OrderLine>(1);

                    if (!done)
                    {
                        float.TryParse(line, out quantity);
                        orderlines.Add(new OrderLine { ProductId = productId, Quantity = quantity });
                    }

                    OrderMessage m = new OrderMessage { PurchaseOrderNumber = poId, PartnerId = partnerId, Done = done, ProvideBy = DateTime.Now + TimeSpan.FromSeconds(10), OrderLines = orderlines };

                    bus.Send(m);

                    Console.WriteLine("Send PO Number {0}.", m.PurchaseOrderNumber);

                    if (done)
                        poId = Guid.NewGuid().ToString();
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }

        private static void Simulate(IBus bus)
        {
            Guid partnerId = Guid.NewGuid();

            int numberOfLines;
            int secondsToProvideBy;

            while(true)
            {
                Random r = new Random();

                numberOfLines = 5 + r.Next(0, 5);
                secondsToProvideBy = 5 + r.Next(0, 5);
                string purchaseOrderNumber = Guid.NewGuid().ToString();

                for (int i = 0; i < numberOfLines; i++)
                {
                    var m = new OrderMessage { 
                        PurchaseOrderNumber = purchaseOrderNumber, 
                        PartnerId = partnerId, 
                        Done = (i == numberOfLines - 1), 
                        ProvideBy = DateTime.Now + TimeSpan.FromSeconds(secondsToProvideBy), 
                        OrderLines = new List<OrderLine> {new OrderLine { ProductId = Guid.NewGuid(), Quantity = (float) (Math.Sqrt(2)*r.Next(10)) } }
                    };

                    bus.Send(m);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
