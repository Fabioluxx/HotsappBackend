﻿using System;
using System.Collections.Generic;

namespace WhatsTroll.Data.Model
{
    public partial class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DateTimeUtc { get; set; }

        public virtual User User { get; set; }
    }
}
