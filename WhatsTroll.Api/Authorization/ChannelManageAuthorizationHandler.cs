﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using WhatsTroll.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WhatsTroll.Api.Authorization
{
    public class ChannelManageAuthorizationHandler :
    AuthorizationHandler<ChannelManageRequirement, int>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       ChannelManageRequirement requirement,
                                                       int resource)
        {
            try
            {
                var identity = context.User.Identity as ClaimsIdentity;
                Claim identityClaim = identity.Claims.FirstOrDefault(c => c.Type == "UserId");
                using (var db = new DataContext())
                {
                    var channel = db.Channel.FirstOrDefault(c => c.Id == resource);
                    if (channel.UserId == int.Parse(identityClaim.Value))
                        context.Succeed(requirement);
                }
            }
            catch (Exception) { }

            return Task.CompletedTask;
        }
    }

    public class ChannelManageRequirement : IAuthorizationRequirement { }
}
