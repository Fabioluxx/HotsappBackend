﻿using FirebaseApi.models;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using System;

namespace FirebaseApi
{
    public class FirebaseController
    {
        private string FIREBASE_KEY;
        public FirebaseController()
        {
            var config = new ConfigurationBuilder()
             .AddJsonFile("config.json")
             .Build();
            FIREBASE_KEY = config["FIREBASE_KEY"];
        }

        private T FirebasePostRequest<T,K>(string ApiMethod, K Data)
        {
            var response = $"https://www.googleapis.com/identitytoolkit/v3/relyingparty/{ApiMethod}?key={FIREBASE_KEY}"
                .PostUrlEncodedAsync(Data)
                .ReceiveJson<T>().Result;
            return response;
        }
        public AccountInfo getAccountInfo(string idToken)
        {
            return FirebasePostRequest<AccountInfo, dynamic>("getAccountInfo",new { idToken });
        }
    }
}
