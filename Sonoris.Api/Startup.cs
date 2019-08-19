﻿using System;
using DbManager.Contexts;
using FirebaseApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sonoris.Api.Authorization;
using Sonoris.Api.Configuration;
using Sonoris.Api.Hubs;
using Sonoris.Api.Hubs.PlayerHub;
using Sonoris.Api.Services;
using Sonoris.Api.Services.SPlaylistMedia;
using Sonoris.Api.Services.Storage;
using YoutubeDataApi;

namespace Sonoris.Api
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

            ConfigureAuth(services);
            ConfigureCors(services);

            services.AddSignalR();

            services.AddDbContext<DataContext>();
            services.AddDbContext<ChannelPlaylistContext>();

            services.AddSingleton<FirebaseController>();
            services.AddSingleton<YoutubeDataService>();
            services.AddSingleton<YoutubeManager>();
            //services.AddSingleton<StorageService>();

            services.AddSingleton<MediaService>();
            services.AddSingleton<PlaylistMediaService>();

            services.AddHostedService<ChannelWorkerHostedService>();
            services.AddSingleton<ChannelWorkerService>();
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
            app.UseMvc();

            app.UseSignalR(route =>
            {
                route.MapHub<PlayerHub>("/PlayerHub");
            });
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
                        .AllowAnyOrigin();
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
                options.AddPolicy("ChannelManage", policy =>
                    policy.Requirements.Add(new ChannelManageRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, ChannelManageAuthorizationHandler>();

        }

    }
}
