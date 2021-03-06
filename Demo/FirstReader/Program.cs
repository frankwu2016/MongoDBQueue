﻿using System;
using System.Linq;
using System.Threading;
using MongoQueue;
using MongoQueue.Autofac;
using MongoQueue.Core;

namespace MongoQueueReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string route = "listener";
            if (args.Any())
            {
                route = args[0];
            }

            var autofacRegistrator = new AutofacRegistrator();
            var configurator = new QueueConfigurator(autofacRegistrator, new MessagingDependencyRegistrator())
                .RegisterHandler<DefaultHandler>();
            var builder = configurator.Build(autofacRegistrator.CreateResolver());

            var subscriber = builder.GetSubscriber();
            subscriber.Subscribe<DefaultHandler, DomainMessage>();

            var mongoMessageListener = builder.GetListener();
            mongoMessageListener.Start(route, CancellationToken.None).Wait();
            Console.WriteLine($"started listener {route}");
            Console.ReadLine();
        }
    }
}