﻿using System;
using FirebaseApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Hotsapp.Api.Configuration;
using Hotsapp.Api.Services;
using Hotsapp.Api.Util;
using Hotsapp.Data.Model;
using Hotsapp.Payment;

namespace Hotsapp.Api
{
    public class Startup
    {
        private readonly IHostingEnvironment _env;
        private readonly IConfiguration _config;
        ILogger<Startup> _logger;

        public Startup(IHostingEnvironment env, IConfiguration config,
            ILogger<Startup> logger)
        {
            _env = env;
            _config = config;
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSignalR();

            ConfigureAuth(services);
            ConfigureCors(services);
            
            var connectionString = _config.GetConnectionString("MySqlConnectionString");
            services.AddDbContext<DataContext>(options => options.UseMySQL(connectionString));

            services.AddSingleton<FirebaseService>();

            services.AddTransient<BalanceService>();
            services.AddTransient<UsernameGeneratorService>();
            services.AddTransient<PaymentService>();
            services.AddTransient<RefreshTokenService>();
        }

        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env)
        {
            app.UseCors("AllowAll");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseAuthentication();
            app.UseHsts();
            app.UseMvc();
        }



        private void ConfigureCors(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithOrigins(_config.GetValue<string>("APP_HOST"));
                    });
            });
        }

        private void ConfigureAuth(IServiceCollection services)
        {
            var signingConfigurations = new SigningConfigurations();
            services.AddSingleton(signingConfigurations);

            services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(bearerOptions =>
            {
                var paramsValidation = bearerOptions.TokenValidationParameters;
                paramsValidation.IssuerSigningKey = signingConfigurations.Key;
                //paramsValidation.ValidAudience = tokenConfigurations.Audience;
                //paramsValidation.ValidIssuer = tokenConfigurations.Issuer;
                paramsValidation.ValidateAudience = false;
                paramsValidation.ValidateIssuer = false;

                paramsValidation.ValidateIssuerSigningKey = true;
                paramsValidation.ValidateLifetime = true;
                paramsValidation.ClockSkew = TimeSpan.Zero;
            });

            services.AddAuthorization(auth =>
            {
                auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser().Build());
            });

            services.AddAuthorization(options =>
            {
            });

        }

    }
}