﻿using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WaClient.Connector.Model;

namespace WaClient.Connector
{
    public interface IWaConnector
    {
        [Post("/api/initialize")]
        Task Initialize(InitializeOptions options);

        [Get("/api/status")]
        Task<StatusModel> GetStatus();

        [Post("/api/sendMessage")]
        Task SendMessage(InitializeOptions options);

        [Get("/api/getMessages")]
        Task GetMessages(InitializeOptions options);
    }
}
