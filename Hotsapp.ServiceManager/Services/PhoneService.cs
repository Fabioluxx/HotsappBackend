﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hotsapp.ServiceManager.Data;

namespace Hotsapp.ServiceManager.Services
{
    public class PhoneService
    {
        ProcessManager _pm;
        public event EventHandler<MessageReceived> OnMessageReceived;
        public PhoneService(ProcessManager pm)
        {
            _pm = pm;
        }

        public async Task Start()
        {
            _pm.OnOutputReceived += Pm_OnOutputReceived;
            _pm.Start();
            await _pm.SendCommand("script -q -c \"yowsup-cli demos --yowsup --config-phone 639552450578 --config-pushname Hotsapp \" /dev/null");
            
            await _pm.SendCommand("");
            await _pm.WaitOutput("offline", 10000);
            Console.WriteLine("READY");
        }

        public async Task Login()
        {
            await _pm.SendCommand("/L");
            await _pm.WaitOutput("Auth: Logged in!");
            Console.WriteLine("LOGIN SUCCESS");
        }

        private void Pm_OnOutputReceived(object sender, string e)
        {
            Console.WriteLine(e);
            var match = Regex.Match(e, "(?<=\t).*");
            if (match.Success)
            {
                var message = match.Value;
                message = message.Substring(1, message.Length - 1);
                var number = Regex.Match(e, "55.+@s.whatsapp").Value.Replace("@s.whatsapp", "");
                Console.WriteLine($"Message: [{number}] {match.Value}");
                var mr = new MessageReceived()
                {
                    Number = number,
                    Message = message
                };
                OnMessageReceived.Invoke(this, mr);
                OnMessage(match.Value);
            }
        }

        private async Task OnMessage(string value)
        {
            value = value.ToLower().Trim();
            switch (value)
            {
                case "viado":
                    await SendMessage("555599436679", "viado eh vc");
                    break;
                case "lixo":
                    await SendMessage("555599436679", "toma no cu");
                    break;
                case "cuzao":
                    await SendMessage("555599436679", "eh vc seu lixoo");
                    break;
                default:
                    await SendMessage("555599436679", "vai se fude");
                    break;
            }
        }

        public async Task SendMessage(string number, string message)
        {
            await _pm.SendCommand($"/message send {number} \"{message}\"");
            await _pm.WaitOutput("Sent:");
        }

        public async Task<bool> IsOnline()
        {
            _pm.SendCommand("");
            _pm.SendCommand("");
            _pm.SendCommand("");
            var offline = _pm.WaitOutput("offline");
            var online = _pm.WaitOutput("connected");
            var timeout = Task.Delay(1000);
            var res = await Task.WhenAny(offline, online, timeout);
            if (res == online)
                return true;
            else
                return false;
        }
    }
}