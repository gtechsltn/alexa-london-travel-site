// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.LondonTravel.Site
{
    using System;
    using System.IO;
    using System.Net.Mime;
    using Extensions;
    using MartinCostello.LondonTravel.Site.Services;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.CookiePolicy;
    using Microsoft.AspNetCore.Cors.Infrastructure;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;
    using NodaTime;
    using Options;
    using Services.Data;
    using Services.Tfl;
    using Telemetry;

    /// <summary>
    /// A class representing the startup logic for the application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
        /// <param name="hostingEnvironment">The <see cref="IWebHostEnvironment"/> to use.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        private IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the current hosting environment.
        /// </summary>
        private IWebHostEnvironment HostingEnvironment { get; }

        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        private IServiceProvider? ServiceProvider { get; set; }

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to use.</param>
        /// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/> to use.</param>
        /// <param name="options">The snapshot of <see cref="SiteOptions"/> to use.</param>
        public void Configure(
            IApplicationBuilder app,
            IHostApplicationLifetime applicationLifetime,
            IOptionsSnapshot<SiteOptions> options)
        {
            ServiceProvider = app.ApplicationServices;

            applicationLifetime.ApplicationStopped.Register(OnApplicationStopped);
            app.UseCustomHttpHeaders(HostingEnvironment, Configuration, options);

            app.UseRequestLocalization("en-GB", "en-US");

            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error")
                   .UseStatusCodePagesWithReExecute("/error", "?id={0}");
            }

            app.UseHsts()
               .UseHttpsRedirection();

            app.UseForwardedHeaders(
                new ForwardedHeadersOptions()
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                });

            app.UseHttpMethodOverride();

            app.UseResponseCompression();

            app.UseStaticFiles(CreateStaticFileOptions());

            app.UseRouting();

            app.UseIdentity(options.Value);

            app.UseEndpoints((endpoints) => endpoints.MapDefaultControllerRoute());

            app.UseSwagger();

            app.UseCookiePolicy(CreateCookiePolicy());
        }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to use.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddOptions();
            services.Configure<SiteOptions>(Configuration.GetSection("Site"));
            services.Configure<ApplicationInsightsServiceOptions>((p) => p.ApplicationVersion = GitMetadata.Commit);

            ConfigureDataProtection(services);

            services.AddAntiforgery(
                (p) =>
                {
                    p.Cookie.Name = ApplicationCookie.Antiforgery.Name;
                    p.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    p.FormFieldName = "_anti-forgery";
                    p.HeaderName = "x-anti-forgery";
                });

            services.AddCors(ConfigureCors);

            services
                .AddLocalization()
                .AddControllersWithViews(ConfigureMvc)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization()
                .AddJsonOptions(ConfigureJsonFormatter);

            services.AddRouting(
                (p) =>
                {
                    p.AppendTrailingSlash = true;
                    p.LowercaseUrls = true;
                });

            services.AddHsts(
                (p) =>
                {
                    p.MaxAge = TimeSpan.FromDays(365);
                    p.IncludeSubDomains = false;
                    p.Preload = false;
                });

            services.AddSwagger(HostingEnvironment);

            services.AddResponseCaching()
                    .AddResponseCompression();

            services.AddSingleton<IClock>((_) => SystemClock.Instance);
            services.AddSingleton<ISiteTelemetry, SiteTelemetry>();
            services.AddSingleton<ITelemetryInitializer, SiteTelemetryInitializer>();
            services.AddSingleton<ITflServiceFactory, TflServiceFactory>();
            services.AddSingleton(DocumentHelpers.CreateClient);

            services.AddSingleton((p) => p.GetRequiredService<IOptions<SiteOptions>>().Value);
            services.AddSingleton((p) => p.GetRequiredService<SiteOptions>().Authentication!.UserStore!);
            services.AddSingleton((p) => p.GetRequiredService<SiteOptions>().Tfl!);

            services.TryAddSingleton<IDocumentService, DocumentService>();
            services.TryAddSingleton<IDocumentCollectionInitializer, DocumentCollectionInitializer>();

            services.AddHttpContextAccessor();
            services.AddScoped<SiteResources>();

            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<ITflService, TflService>();

            services.AddPolly();
            services.AddHttpClients();

            services.AddApplicationAuthentication(Configuration);
        }

        /// <summary>
        /// Configures the JSON serializer for MVC.
        /// </summary>
        /// <param name="options">The <see cref="JsonOptions"/> to configure.</param>
        internal static void ConfigureJsonFormatter(JsonOptions options)
        {
            // Omit nulls to reduce payload size
            options.JsonSerializerOptions.IgnoreNullValues = true;

            // Make JSON easier to read for debugging at the expense of larger payloads
            options.JsonSerializerOptions.WriteIndented = true;

            // Opt-out of case insensitivity on property names
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
        }

        /// <summary>
        /// Creates the <see cref="CookiePolicyOptions"/> to use.
        /// </summary>
        /// <returns>
        /// The <see cref="CookiePolicyOptions"/> to use for the application.
        /// </returns>
        private static CookiePolicyOptions CreateCookiePolicy()
        {
            return new CookiePolicyOptions()
            {
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.Always,
            };
        }

        /// <summary>
        /// Sets the cache headers for static files.
        /// </summary>
        /// <param name="context">The static file response context to set the headers for.</param>
        private void SetCacheHeaders(StaticFileResponseContext context)
        {
            var maxAge = TimeSpan.FromDays(7);

            if (context.File.Exists && HostingEnvironment.IsProduction())
            {
                string? extension = Path.GetExtension(context.File.PhysicalPath);

                // These files are served with a content hash in the URL so can be cached for longer
                bool isScriptOrStyle =
                    string.Equals(extension, ".css", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".js", StringComparison.OrdinalIgnoreCase);

                if (isScriptOrStyle)
                {
                    maxAge = TimeSpan.FromDays(365);
                }
            }

            var headers = context.Context.Response.GetTypedHeaders();
            headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = maxAge,
            };
        }

        /// <summary>
        /// Configures the options for serving static content.
        /// </summary>
        /// <returns>
        /// The <see cref="StaticFileOptions"/> to use.
        /// </returns>
        private StaticFileOptions CreateStaticFileOptions()
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".webmanifest"] = "application/manifest+json";

            return new StaticFileOptions()
            {
                ContentTypeProvider = provider,
                DefaultContentType = MediaTypeNames.Application.Json,
                OnPrepareResponse = SetCacheHeaders,
                ServeUnknownFileTypes = true,
            };
        }

        /// <summary>
        /// Configures CORS.
        /// </summary>
        /// <param name="corsOptions">The <see cref="CorsOptions"/> to configure.</param>
        private void ConfigureCors(CorsOptions corsOptions)
        {
            var siteOptions = ServiceProvider!.GetService<SiteOptions>();

            corsOptions.AddPolicy(
                "DefaultCorsPolicy",
                (builder) =>
                {
                    builder
                        .WithExposedHeaders(siteOptions?.Api?.Cors?.ExposedHeaders ?? Array.Empty<string>())
                        .WithHeaders(siteOptions?.Api?.Cors?.Headers ?? Array.Empty<string>())
                        .WithMethods(siteOptions?.Api?.Cors?.Methods ?? Array.Empty<string>());

                    if (HostingEnvironment.IsDevelopment())
                    {
                        builder.AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(siteOptions?.Api?.Cors?.Origins ?? Array.Empty<string>());
                    }
                });
        }

        /// <summary>
        /// Configures Data Protection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        private void ConfigureDataProtection(IServiceCollection services)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            string environment = Configuration.AzureEnvironment().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase

            var dataProtection = services
                .AddDataProtection()
                .SetApplicationName($"londontravel-{environment}");

            string connectionString = Configuration.AzureStorageConnectionString();

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                string relativePath = $"/london-travel/{environment}/keys.xml";
                dataProtection.PersistKeysToAzureBlobStorage(
                    connectionString,
                    "data-protection",
                    relativePath);
            }
        }

        /// <summary>
        /// Configures MVC.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/> to configure.</param>
        private void ConfigureMvc(MvcOptions options)
        {
            options.Conventions.Add(new Swagger.ApiExplorerDisplayConvention());

            if (!HostingEnvironment.IsDevelopment())
            {
                options.Filters.Add(new RequireHttpsAttribute());
            }
        }

        /// <summary>
        /// Handles the application being stopped.
        /// </summary>
        private void OnApplicationStopped()
        {
            Serilog.Log.CloseAndFlush();
        }
    }
}
