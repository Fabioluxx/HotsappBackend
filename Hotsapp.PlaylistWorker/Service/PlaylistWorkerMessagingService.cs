﻿using Hotsapp.Messaging;
using RabbitMQ.Client;
using Serilog;
using System.Text;

namespace Hotsapp.PlaylistWorker
{
    public class PlaylistWorkerMessagingService
    {
        private ILogger _log = Log.ForContext<PlaylistWorkerMessagingService>();
        private IModel channel;

        public PlaylistWorkerMessagingService()
        {
            channel = MessagingFactory.GetConnection().CreateModel();
            channel.ExchangeDeclare("channel-playevent", ExchangeType.Topic);
        }

        public void PublishForTag(string data, string routingKey)
        {
            var body = Encoding.UTF8.GetBytes(data);

            channel.BasicPublish(exchange: "channel-playevent",
                                 routingKey: routingKey,
                                 basicProperties: null,
                                 body: body);
        }
    }
}
