// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.LondonTravel.Site.Swagger;
using Swashbuckle.AspNetCore.Annotations;

namespace MartinCostello.LondonTravel.Site.Extensions;

/// <summary>
/// A class containing extension methods for the <see cref="RouteHandlerBuilder"/> class. This class cannot be inherited.
/// </summary>
internal static class RouteHandlerBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="SwaggerResponseAttribute"/> to the metadata for all builders produced by builder.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="description">The response description.</param>
    /// <param name="statusCode">The response status code. Defaults to <see cref="StatusCodes.Status200OK"/>.</param>
    /// <param name="contentType">The response content type. Defaults to <c>application/json</c>.</param>
    /// <returns>
    /// A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.
    /// </returns>
    internal static RouteHandlerBuilder Produces<TResponse>(
        this RouteHandlerBuilder builder,
        string description,
        int statusCode = StatusCodes.Status200OK,
        string? contentType = null)
    {
        return builder
            .Produces<TResponse>(statusCode, contentType)
            .WithMetadata(new SwaggerResponseAttribute(statusCode) { Type = typeof(TResponse), Description = description });
    }

    /// <summary>
    /// Adds <see cref="SwaggerResponseAttribute"/> to the metadata for all builders produced by builder.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <typeparam name="TResponseExampleProvider">The type of the example provider for the response.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="description">The response description.</param>
    /// <param name="statusCode">The response status code. Defaults to <see cref="StatusCodes.Status200OK"/>.</param>
    /// <param name="contentType">The response content type. Defaults to <c>application/json</c>.</param>
    /// <returns>
    /// A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.
    /// </returns>
    internal static RouteHandlerBuilder Produces<TResponse, TResponseExampleProvider>(
        this RouteHandlerBuilder builder,
        string description,
        int statusCode = StatusCodes.Status200OK,
        string? contentType = null)
        where TResponseExampleProvider : IExampleProvider<TResponse>
    {
        return builder
            .Produces<TResponse>(description, statusCode, contentType)
            .WithResponseExample<TResponse, TResponseExampleProvider>();
    }

    /// <summary>
    /// Adds the <see cref="SwaggerOperationAttribute"/> to the metadata for all builders produced by builder.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="summary">The operation summary.</param>
    /// <param name="description">The optional operation description.</param>
    /// <returns>
    /// A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.
    /// </returns>
    internal static RouteHandlerBuilder WithOperationDescription(
        this RouteHandlerBuilder builder,
        string summary,
        string? description = null)
    {
        return builder.WithMetadata(new SwaggerOperationAttribute(summary, description));
    }

    /// <summary>
    /// Adds the <see cref="SwaggerResponseExampleAttribute"/> to the metadata for all builders produced by builder.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <typeparam name="TExampleProvider">The type of the example provider.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>
    /// A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.
    /// </returns>
    internal static RouteHandlerBuilder WithResponseExample<TResponse, TExampleProvider>(this RouteHandlerBuilder builder)
        where TExampleProvider : IExampleProvider<TResponse>
    {
        return builder.WithMetadata(new SwaggerResponseExampleAttribute(typeof(TResponse), typeof(TExampleProvider)));
    }
}
