// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.LondonTravel.Site.Identity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AspNet.Security.OAuth.Amazon;
    using AspNet.Security.OAuth.Apple;
    using AspNet.Security.OAuth.GitHub;
    using Azure.Security.KeyVault.Secrets;
    using MartinCostello.LondonTravel.Site.Options;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Facebook;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.AspNetCore.Authentication.OAuth;
    using Microsoft.AspNetCore.Authentication.Twitter;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder TryAddAmazon(
            this AuthenticationBuilder builder,
            Options.AuthenticationOptions options)
        {
            string name = "Amazon";

            if (IsProviderEnabled(name, options))
            {
                builder.AddAmazon()
                       .Configure<AmazonAuthenticationOptions>(name);
            }

            return builder;
        }

        public static AuthenticationBuilder TryAddApple(
            this AuthenticationBuilder builder,
            Options.AuthenticationOptions options)
        {
            string name = "Apple";

            if (IsProviderEnabled(name, options, requiresClientSecret: false))
            {
                builder.AddApple()
                       .Configure<AppleAuthenticationOptions>(name, (providerOptions, serviceProvider) =>
                       {
                           var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                           var secretClient = serviceProvider.GetService<SecretClient>();

                           providerOptions.KeyId = configuration[$"Site:Authentication:ExternalProviders:{name}:KeyId"];
                           providerOptions.TeamId = configuration[$"Site:Authentication:ExternalProviders:{name}:TeamId"];

                           if (secretClient is not null)
                           {
                               providerOptions.GenerateClientSecret = true;
                               providerOptions.PrivateKeyBytes = async (keyId) =>
                               {
                                   var secret = await secretClient.GetSecretAsync($"AuthKey-{keyId}");

                                   string privateKey = secret.Value.Value;

                                   if (privateKey.StartsWith("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal))
                                   {
                                       string[] lines = privateKey.Split('\n');
                                       privateKey = string.Join(string.Empty, lines[1..^1]);
                                   }

                                   return Convert.FromBase64String(privateKey);
                               };
                           }
                       });
            }

            return builder;
        }

        public static AuthenticationBuilder TryAddFacebook(
            this AuthenticationBuilder builder,
            Options.AuthenticationOptions options)
        {
            string name = "Facebook";

            if (IsProviderEnabled(name, options))
            {
                builder.AddFacebook()
                       .Configure<FacebookOptions>(name);
            }

            return builder;
        }

        public static AuthenticationBuilder TryAddGitHub(
            this AuthenticationBuilder builder,
            Options.AuthenticationOptions options)
        {
            string name = "GitHub";

            if (IsProviderEnabled(name, options))
            {
                builder.AddGitHub()
                       .Configure<GitHubAuthenticationOptions>(name, (p, _) => p.Scope.Add("user:email"));
            }

            return builder;
        }

        public static AuthenticationBuilder TryAddGoogle(
            this AuthenticationBuilder builder,
            Options.AuthenticationOptions options)
        {
            string name = "Google";

            if (IsProviderEnabled(name, options))
            {
                builder.AddGoogle()
                       .Configure<GoogleOptions>(name);
            }

            return builder;
        }

        public static AuthenticationBuilder TryAddMicrosoft(
            this AuthenticationBuilder builder,
            Options.AuthenticationOptions options)
        {
            string name = "Microsoft";

            if (IsProviderEnabled(name, options))
            {
                builder.AddMicrosoftAccount()
                       .Configure<MicrosoftAccountOptions>(name);
            }

            return builder;
        }

        public static AuthenticationBuilder TryAddTwitter(
            this AuthenticationBuilder builder,
            Options.AuthenticationOptions options)
        {
            string name = "Twitter";

            if (IsProviderEnabled(name, options))
            {
                builder.AddTwitter()
                    .Services
                    .AddOptions<TwitterOptions>(name)
                    .Configure<IServiceProvider>((options, serviceProvider) =>
                    {
                        var siteOptions = serviceProvider.GetRequiredService<SiteOptions>();
                        var provider = siteOptions!.Authentication!.ExternalProviders![name] !;

                        options.ConsumerKey = provider.ClientId;
                        options.ConsumerSecret = provider.ClientSecret;
                        options.RetrieveUserDetails = true;
                        options.StateCookie.Name = ApplicationCookie.State.Name;

                        options.Events.OnRemoteFailure =
                            (context) => HandleRemoteFailure(
                                context,
                                options.SignInScheme!,
                                options.StateDataFormat,
                                context.HttpContext.RequestServices.GetRequiredService<ILogger<TwitterOptions>>(),
                                (token) => token?.Properties?.Items);

                        ConfigureRemoteAuthentication("Twitter", options, serviceProvider);

                        var externalEvents = serviceProvider.GetService<ExternalAuthEvents>();

                        if (externalEvents?.OnRedirectToTwitterAuthorizationEndpoint is not null)
                        {
                            options.Events.OnRedirectToAuthorizationEndpoint = externalEvents.OnRedirectToTwitterAuthorizationEndpoint;
                        }
                    });
            }

            return builder;
        }

        public static Task HandleRemoteFailure<T>(
            RemoteFailureContext context,
            string provider,
            ISecureDataFormat<T> secureDataFormat,
            ILogger logger,
            Func<T, IDictionary<string, string?>?> propertiesProvider)
        {
            string? path = GetSiteErrorRedirect(context, secureDataFormat, propertiesProvider);

            if (string.IsNullOrEmpty(path) ||
                !Uri.TryCreate(path, UriKind.Relative, out Uri? notUsed))
            {
                path = "/";
            }

            SiteMessage message;

            if (WasPermissionDenied(context))
            {
                message = SiteMessage.LinkDenied;
                logger.LogTrace("User denied permission.");
            }
            else
            {
                message = SiteMessage.LinkFailed;

                var eventId = default(EventId);
                string errors = string.Join(";", context.Request.Query.Select((p) => $"'{p.Key}' = '{p.Value}'"));
                string logMessage = $"Failed to sign-in using '{provider}': '{context.Failure?.Message}'. Errors: {errors}.";

                if (IsCorrelationFailure(context))
                {
                    // Not a server-side problem, so do not create log noise
                    logger.LogTrace(eventId, context.Failure, logMessage);
                }
                else
                {
                    logger.LogError(eventId, context.Failure, logMessage);
                }
            }

            context.Response.Redirect($"{path}?Message={message}");
            context.HandleResponse();

            return Task.CompletedTask;
        }

        private static bool IsProviderEnabled(
            string name,
            Options.AuthenticationOptions options,
            bool requiresClientSecret = true)
        {
            ExternalSignInOptions? provider = null;

            if (options.ExternalProviders?.TryGetValue(name, out provider) != true ||
                provider is null)
            {
                return false;
            }

            return
                provider.IsEnabled &&
                !string.IsNullOrEmpty(provider.ClientId) &&
                (!requiresClientSecret || !string.IsNullOrEmpty(provider.ClientSecret));
        }

        private static void Configure<T>(
            this AuthenticationBuilder builder,
            string name,
            Action<T, IServiceProvider>? configure = null)
            where T : OAuthOptions
        {
            builder.Services
                .AddOptions<T>(name)
                .Configure<IServiceProvider>((options, serviceProvider) =>
                {
                    var siteOptions = serviceProvider.GetRequiredService<SiteOptions>();
                    var provider = siteOptions!.Authentication!.ExternalProviders![name] !;

                    options.ClientId = provider.ClientId;
                    options.ClientSecret = provider.ClientSecret;

                    ConfigureRemoteAuthentication(name, options, serviceProvider);

                    options.Events.OnRemoteFailure = (context) =>
                    {
                        return HandleRemoteFailure(
                            context,
                            context.Scheme.Name,
                            options.StateDataFormat,
                            context.HttpContext.RequestServices.GetRequiredService<ILogger<T>>(),
                            (p) => p.Items);
                    };

                    options.Events.OnTicketReceived = (context) =>
                    {
                        var clock = context.HttpContext.RequestServices.GetRequiredService<ISystemClock>();

                        context.Properties.ExpiresUtc = clock.UtcNow.AddDays(150);
                        context.Properties.IsPersistent = true;

                        return Task.CompletedTask;
                    };

                    var externalEvents = serviceProvider.GetService<ExternalAuthEvents>();

                    if (externalEvents?.OnRedirectToOAuthAuthorizationEndpoint is not null)
                    {
                        options.Events.OnRedirectToAuthorizationEndpoint = externalEvents.OnRedirectToOAuthAuthorizationEndpoint;
                    }

                    configure?.Invoke(options, serviceProvider);
                });
        }

        private static void ConfigureRemoteAuthentication<T>(string name, T options, IServiceProvider serviceProvider)
            where T : RemoteAuthenticationOptions
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            options.Backchannel = httpClientFactory.CreateClient(name);
            options.CorrelationCookie.Name = ApplicationCookie.Correlation.Name;
        }

        private static string? GetSiteErrorRedirect<T>(
            RemoteFailureContext context,
            ISecureDataFormat<T> secureDataFormat,
            Func<T, IDictionary<string, string?>?> propertiesProvider)
        {
            var state = context.Request.Query["state"];
            var stateData = secureDataFormat.Unprotect(state);
            var properties = propertiesProvider?.Invoke(stateData!);

            if (properties == null ||
                !properties.TryGetValue(SiteContext.ErrorRedirectPropertyName, out string? value))
            {
                value = null;
            }

            return value;
        }

        private static bool WasPermissionDenied(RemoteFailureContext context)
        {
            string? error = context.Request.Query["error"].FirstOrDefault();

            if (string.Equals(error, "access_denied", StringComparison.Ordinal) ||
                string.Equals(error, "consent_required", StringComparison.Ordinal))
            {
                return true;
            }

            string? reason = context.Request.Query["error_reason"].FirstOrDefault();

            if (string.Equals(reason, "user_denied", StringComparison.Ordinal))
            {
                return true;
            }

            string? description = context.Request.Query["error_description"].FirstOrDefault();

            if (!string.IsNullOrEmpty(description) &&
                description.Contains("denied", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return context.Request.Query.ContainsKey("denied");
        }

        private static bool IsCorrelationFailure(RemoteFailureContext context)
        {
            // See https://github.com/aspnet/Security/blob/ad425163b29b1e09a41e84423b0dcbac797c9164/src/Microsoft.AspNetCore.Authentication.OAuth/OAuthHandler.cs#L66
            // and https://github.com/aspnet/Security/blob/2d1c56ce5ccfc15c78dd49cee772f6be473f3ee2/src/Microsoft.AspNetCore.Authentication/RemoteAuthenticationHandler.cs#L203
            // This effectively means that the user did not pass their cookies along correctly to correlate the request.
            return string.Equals(context.Failure?.Message, "Correlation failed.", StringComparison.Ordinal);
        }
    }
}