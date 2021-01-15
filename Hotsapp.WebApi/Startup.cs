using FirebaseApi;
using Hotsapp.WebApi.Configuration;
using Hotsapp.WebApi.Services;
using Hotsapp.WebApi.Services.Youtube;
using Hotsapp.WebStreamer.Hubs;
using Hotsapp.WebStreamer.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;

namespace Hotsapp.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console()
            .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR()
                .AddNewtonsoftJsonProtocol(options =>
                {
                    options.PayloadSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFZ" });
                });

            services.AddControllers()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.Converters.Add(
                        new Newtonsoft.Json.Converters.IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFZ" }
                        ));
            services.AddMvc();

            ConfigureAuth(services);

            services.AddData(Configuration.GetConnectionString("MySql"));
            services.AddMongoDB(Configuration.GetConnectionString("MongoDB"));
            services.AddSingleton<YouTubeCacheService>();
            services.AddSingleton<YoutubeClientService>();
            services.AddSingleton<ChannelService>();

            services.AddSingleton(new FirebaseService(Configuration["FirebaseApiKey"]));

            //STREAMER HUB
            services.AddTransient<StreamWorker>();
            services.AddSingleton<StreamWorkerFactory>();
            //Create a HostedService in a way that we can use it in DI
            services.AddSingleton<StreamerService>();
            services.AddHostedService(sp => sp.GetRequiredService<StreamerService>());

            services.Configure<HostOptions>(option =>
            {
                option.ShutdownTimeout = System.TimeSpan.FromSeconds(20);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(options =>
                options.AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowAnyOrigin());

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<StreamHub>("/streamhub", options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
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
