// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MartinCostello.LondonTravel.Site.Swagger;

namespace MartinCostello.LondonTravel.Site.Models;

/// <summary>
/// Represents an error from an API resource.
/// </summary>
[SwaggerTypeExample(typeof(ErrorResponseExampleProvider))]
public sealed class ErrorResponse
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    [Required]
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request Id.
    /// </summary>
    [JsonPropertyName("requestId")]
    [Required]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error details, if any.
    /// </summary>
    [JsonPropertyName("details")]
    [Required]
    public ICollection<string> Details { get; set; } = new List<string>();
}
