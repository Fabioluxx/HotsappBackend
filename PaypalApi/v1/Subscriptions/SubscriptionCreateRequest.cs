﻿// This class was generated on Mon, 09 Apr 2018 18:07:49 UTC by version 0.1.0-dev+291f3f of Braintree SDK Generator
// AuthorizationGetRequest.cs
// @version 0.1.0-dev+291f3f
// @type request
// @data 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
// DO NOT EDIT
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using BraintreeHttp;
using System.Runtime.Serialization;

namespace PayPal.v1.Subscriptions
{
    public class SubscriptionCreateRequest : HttpRequest
    {
        public SubscriptionCreateRequest() : base("/v1/billing/subscriptions?", HttpMethod.Post, typeof(Subscription))
        {

            this.ContentType = "application/json";
        }

        public SubscriptionCreateRequest RequestBody(string planId)
        {
            this.Body = new CreateSubscriptionBody() { PlanId = planId };
            return this;
        }
    }

    [DataContract]
    public class CreateSubscriptionBody
    {
        public CreateSubscriptionBody() { }

        [DataMember(Name = "plan_id", EmitDefaultValue = false)]
        public string PlanId;
    }
}
