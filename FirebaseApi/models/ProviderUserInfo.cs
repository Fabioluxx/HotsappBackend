﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseApi
{
    public class ProviderUserInfo
    {
        public string providerId { get; set; }
        public string displayName { get; set; }
        public string photoUrl { get; set; }
        public string federatedId { get; set; }
        public string email { get; set; }
        public string rawId { get; set; }
    }
}
