// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MartinCostello.LondonTravel.Site.Services.Tfl;

namespace MartinCostello.LondonTravel.Site.Extensions;

[ExcludeFromCodeCoverage]
[JsonSerializable(typeof(ICollection<LineInfo>))]
[JsonSerializable(typeof(ICollection<StopPoint>))]
internal sealed partial class ApplicationJsonSerializerContext : JsonSerializerContext
{
}
