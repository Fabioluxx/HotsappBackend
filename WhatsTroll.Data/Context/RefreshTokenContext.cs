﻿using Microsoft.EntityFrameworkCore;
using WhatsTroll.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsTroll.Data.Context
{
    public class RefreshTokenContext : DataContext
    {
        public RefreshToken CreateRefreshToken(int User)
        {
            String token = Guid.NewGuid().ToString().Replace("-", String.Empty);
            RefreshToken refresh = new RefreshToken()
            {
                Id = token,
                UserId = User,
                IsRevoked = false
            };
            using (var context = DataFactory.CreateNew())
            {
                context.RefreshToken.Add(refresh);
                context.SaveChanges();
            }
            return refresh;
        }

        public RefreshToken GetToken(String token)
        {
            using (var context = DataFactory.CreateNew())
            {
                var item = context.RefreshToken.Where(t => t.Id == token)
                    .Include(t => t.User)
                    .FirstOrDefault();
                return item;
            }
        }

        public void RevokeToken(String token)
        {
            using (var context = DataFactory.CreateNew())
            {
                var item = context.RefreshToken.Where(t => t.Id == token && !t.IsRevoked).FirstOrDefault();
                item.IsRevoked = true;
                context.RefreshToken.Update(item);
                context.SaveChanges();
            }
        }
    }
}
