﻿using System;
using System.Collections.Generic;

namespace Hotsapp.Data.Model
{
    public partial class Payment
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string PaypalOrderId { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public int UserId { get; set; }
        public int? SubscriptionId { get; set; }

        public virtual User User { get; set; }
    }
}
