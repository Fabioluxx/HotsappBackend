﻿using Hotsapp.Data.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hotsapp.Data.Model;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.IO;
using Serilog.Context;
using Microsoft.Extensions.Logging;

namespace Hotsapp.ServiceManager.Services
{
    public class ServiceUpdater : IHostedService, IDisposable
    {
        PhoneService _phoneService;
        NumberManager _numberManager;
        private Timer _timer;
        private bool updateRunning = false;
        private DateTime? lastLoginAttempt = null;
        private bool isOnline = false;
        private int offlineCount = 0;
        private IHostingEnvironment _hostingEnvironment;
        private ILogger<ServiceUpdater> _log;
        private DateTime lastUpdate = DateTime.UtcNow;

        public ServiceUpdater(PhoneService phoneService, NumberManager numberManager, IHostingEnvironment hostingEnvironment, ILogger<ServiceUpdater> log)
        {
            _phoneService = phoneService;
            _numberManager = numberManager;
            _hostingEnvironment = hostingEnvironment;
            _log = log;
        }

        bool runningMessageSender = false;
        public async Task CheckMessagesToSend()
        {
            if (runningMessageSender)
                return;
            runningMessageSender = true;

            try
            {
                using (var context = DataFactory.GetContext())
                {
                    var message = context.Message.Where(m => m.IsInternal && !m.Processed && m.InternalNumber == _numberManager.currentNumber)
                        .OrderBy(m => m.Id)
                        .FirstOrDefault();
                    if (message != null)
                    {
                        int maxAttempts = 5;
                        var success = false;
                        for (int i = 1; i <= maxAttempts; i++)
                        {
                            try
                            {
                                _log.LogInformation("Sending new message! Attempt: {0} of {1}", i, maxAttempts);
                                success = await _phoneService.SendMessage(message.ExternalNumber, message.Content);
                                if (!success)
                                    throw new Exception("Cannot send message");
                                break;
                            }
                            catch (Exception e)
                            {
                                _log.LogError(e, "Failed to send message");
                                await Task.Delay(3000);
                            }
                        }
                        message.Processed = true;
                        message.Error = !success;
                        await context.SaveChangesAsync();
                    }
                }
            }catch(Exception e)
            {
                _log.LogError(e, "Error running MessageSender");
            }

            runningMessageSender = false;
        }

        public void OnMessageReceived(object sender, Data.MessageReceived mr)
        {
            using (var context = DataFactory.GetContext())
            {
                var number = context.VirtualNumber.SingleOrDefault(n => n.Number == _numberManager.currentNumber);
                var message = new Message()
                {
                    Content = mr.Message,
                    ExternalNumber = mr.Number,
                    InternalNumber = _numberManager.currentNumber,
                    DateTimeUtc = DateTime.UtcNow,
                    IsInternal = false,
                    UserId = number.OwnerId
                };
                context.Message.Add(message);
                context.SaveChanges();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(Init);
            return Task.CompletedTask;
        }

        public async Task Init()
        {
            _log.LogInformation("Starting ServiceUpdater");
            updateRunning = false;
            while (true)
            {
                await Task.Delay(3000);
                try
                {
                    _log.LogInformation("Trying to allocate number");
                    _numberManager.TryAllocateNumber().Wait();
                    if (_numberManager.currentNumber != null)
                        break;
                }catch(Exception e)
                {
                    _log.LogError(e, "Error allocating number");
                }
                _log.LogInformation("Cannot allocate any number, waiting...");
            }

            LogContext.PushProperty("PhoneNumber", _numberManager.currentNumber);

            _numberManager.LoadData();

            UpdateTask(null);
            _phoneService.OnMessageReceived += OnMessageReceived;

            _phoneService.Start().Wait();
            var loginSuccess = await _phoneService.Login();
            if (!loginSuccess)
            {
                await _numberManager.SetNumberError("login_error");
                await StopAsync(new CancellationToken());
            }

            lastLoginAttempt = DateTime.UtcNow;
            if (_hostingEnvironment.IsProduction())
                _phoneService.SetProfilePicture().Wait();
            _phoneService.SetStatus().Wait();

            _timer = new Timer(UpdateTask, null, TimeSpan.Zero,
                TimeSpan.FromMilliseconds(800));

            new Timer(CheckDeadService, null, TimeSpan.Zero,
                TimeSpan.FromMilliseconds(1000));
        }

        private void CheckDeadService(object state)
        {
            if (lastUpdate < DateTime.UtcNow.AddMinutes(-1))
            {
                _log.LogInformation("DeadServiceCherker - Current Service is Dead, Stopping...");
                StopAsync(new CancellationToken()).Wait();
            }
                
        }

        private void UpdateTask(object state)
        {
            if (updateRunning)
                return;
            updateRunning = true;
            _log.LogInformation("Run Update Check");
            lastUpdate = DateTime.UtcNow;

            try
            {
                isOnline = _phoneService.IsOnline().Result;
                _log.LogInformation("Number is online: {0}", isOnline);
            }
            catch (Exception e)
            {
                _log.LogError(e, "ServiceUpdater IsOnline Check Error");
                isOnline = false;
            }

            try
            {
                _numberManager.PutCheck().Wait();
                if (_numberManager.ShouldStop().Result)
                {
                    _log.LogInformation("Automatically stopping ServiceUpdater");
                    
                    StopAsync(new CancellationToken()).Wait();

                    return;
                }
                if(isOnline)
                    CheckMessagesToSend().Wait();
            }
            catch(Exception e)
            {
                _log.LogError(e, "ServiceUpdater Error");
                isOnline = false;
            }

            if (isOnline)
                offlineCount = 0;
            else
                offlineCount++;

            try
            {
                CheckDisconnection().Wait();
            }
            catch (Exception e)
            {
                _log.LogError(e, "ServiceUpdater CheckDisconnection Error");
            }

            updateRunning = false;
        }

        private async Task CheckDisconnection()
        {
            var minTimeToCheckAgain = DateTime.UtcNow.AddSeconds(-15);
            if (lastLoginAttempt == null || lastLoginAttempt > minTimeToCheckAgain)
                return;

            if (offlineCount > 20)
            {
                _log.LogInformation("OfflineCount exceeded limit, stopping service");
                try
                {
                    _phoneService.Stop();
                }catch(Exception e)
                {
                    _log.LogError(e, "Error stopping service");
                }
                Environment.Exit(-1);
            }

            /*
            if (_phoneService.isDead || (offlineCount >= 5 && offlineCount <= 10))
            {
                _log.LogInformation("[Connection Checker] PhoneService IsDead! Reconnecting.");
                _phoneService.Stop();
                await _phoneService.Start();
                await _phoneService.Login();
                lastLoginAttempt = DateTime.UtcNow;
                offlineCount = 0;
                return;
            }*/

            /*
            if (offlineCount >= 5 && offlineCount <= 10)
            {
                _log.LogInformation("[Connection Checker] PhoneService is offline! Reconnecting.");
                await _phoneService.Login();
                lastLoginAttempt = DateTime.UtcNow;
                return;
            }*/
            
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Timed Background Service is stopping.");
            try
            {
                _phoneService.Stop();
                _timer?.Change(Timeout.Infinite, 0);
                _numberManager.ReleaseNumber().Wait();
            }catch(Exception e)
            {
                _log.LogError(e, "Error Stopping ServiceUpdater");
            }

            LogContext.PushProperty("PhoneNumber", null);

            Environment.Exit(-1);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
