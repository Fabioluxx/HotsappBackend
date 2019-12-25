﻿using Dapper;
using Hotsapp.Data.Util;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hotsapp.Api.Services
{
    public class DbTasksService : IHostedService, IDisposable
    {
        private Timer _timer;
        private Timer _cleanerTimer;

        public DbTasksService()
        {

        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Starting DbTasksService");

            _timer = new Timer(RunTasks, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(1));
            _cleanerTimer = new Timer(CleanOldBackups, null, TimeSpan.Zero,
                TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Stopping DbTasksService");

            _timer?.Change(Timeout.Infinite, 0);
            _cleanerTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _cleanerTimer?.Dispose();
        }

        private void RunTasks(object state)
        {

        }

        private void CleanOldBackups(object state)
        {
            using (var conn = DataFactory.OpenConnection())
            {
                conn.Query(@"DELETE vd.* FROM hotsapp.virtual_number_data vd
  LEFT JOIN (SELECT vd.Number, MAX(vd.Id) AS LastId FROM hotsapp.virtual_number_data vd
  GROUP BY vd.Number) tk ON tk.LastId = vd.Id
  WHERE tk.LastId IS NULL");
            }
        }

    }
}
