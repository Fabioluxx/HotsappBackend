﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlaylistWorker
{
    public class MessagingService : IHostedService
    {
        private ILogger<MessagingService> _log;
        private readonly string connectionString;
        private ConnectionFactory factory;
        private IConnection connection;
        private IModel channel;

        public MessagingService(ILogger<MessagingService> log, IConfiguration config)
        {
            _log = log;
            connectionString = config.GetConnectionString("RabbitMQ");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Messaging service starting");
            factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            _log.LogInformation("Messaging service connected: {0}", connection.IsOpen);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Messaging service stopping");
            connection?.Close();
            channel?.Close();
        }

        public void PublishForTag(string data, string tag)
        {
            var body = Encoding.UTF8.GetBytes(data);

            channel.BasicPublish(exchange: tag,
                                 routingKey: "",
                                 basicProperties: null,
                                 body: body);
        }
    }
}
