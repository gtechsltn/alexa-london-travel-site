// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using MartinCostello.LondonTravel.Site.Identity;
using MartinCostello.LondonTravel.Site.Models;
using MartinCostello.LondonTravel.Site.Services;
using MartinCostello.LondonTravel.Site.Services.Data;
using MartinCostello.LondonTravel.Site.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MartinCostello.LondonTravel.Site.Controllers;

/// <summary>
/// A class containing tests for the <see cref="ApiController"/> class. This class cannot be inherited.
/// </summary>
public static class ApiControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public static async Task Preferences_Returns_Unauthorized_Json_If_No_Authorization_Header_Specified(string authorizationHeader)
    {
        // Arrange
        ApiController target = CreateTarget();

        // Act
        var actual = await target.GetPreferences(authorizationHeader, default);

        // Assert
        actual.ShouldNotBeNull();

        actual.Result.ShouldNotBeNull();
        var objectResult = actual.Result.ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(401);
        objectResult.Value.ShouldNotBeNull();

        var data = objectResult.Value.ShouldBeOfType<ErrorResponse>();

        data.Message.ShouldBe("No access token specified.");
        data.StatusCode.ShouldBe(401);
        data.Details.ShouldNotBeNull();
        data.Details.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("not;auth")]
    public static async Task Preferences_Returns_Unauthorized_Json_If_Authorization_Header_Is_Invalid(string authorizationHeader)
    {
        // Arrange
        ApiController target = CreateTarget();

        // Act
        var actual = await target.GetPreferences(authorizationHeader, default);

        // Assert
        actual.ShouldNotBeNull();

        actual.Result.ShouldNotBeNull();
        var objectResult = actual.Result.ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(401);
        objectResult.Value.ShouldNotBeNull();

        var data = objectResult.Value.ShouldBeOfType<ErrorResponse>();

        data.Message.ShouldBe("Unauthorized.");
        data.StatusCode.ShouldBe(401);
        data.Details.ShouldNotBeNull();
        data.Details.ShouldHaveSingleItem();
        data.Details.ShouldContain("The provided authorization value is not valid.");
    }

    [Theory]
    [InlineData("bearer")]
    [InlineData("bearer ")]
    public static async Task Preferences_Returns_Unauthorized_Json_If_Authorization_Header_Has_No_Value(string authorizationHeader)
    {
        // Arrange
        ApiController target = CreateTarget();

        // Act
        var actual = await target.GetPreferences(authorizationHeader, default);

        // Assert
        actual.ShouldNotBeNull();

        actual.Result.ShouldNotBeNull();
        var objectResult = actual.Result.ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(401);
        objectResult.Value.ShouldNotBeNull();

        var data = objectResult.Value.ShouldBeOfType<ErrorResponse>();

        data.Message.ShouldBe("Unauthorized.");
        data.StatusCode.ShouldBe(401);
        data.Details.ShouldNotBeNull();
        data.Details.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("something token")]
    [InlineData("unknown token")]
    public static async Task Preferences_Returns_Unauthorized_Json_If_Authorization_Header_Is_Incorrect_Scheme(string authorizationHeader)
    {
        // Arrange
        ApiController target = CreateTarget();

        // Act
        var actual = await target.GetPreferences(authorizationHeader, default);

        // Assert
        actual.ShouldNotBeNull();

        actual.Result.ShouldNotBeNull();
        var objectResult = actual.Result.ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(401);
        objectResult.Value.ShouldNotBeNull();

        var data = objectResult.Value.ShouldBeOfType<ErrorResponse>();

        data.Message.ShouldBe("Unauthorized.");
        data.StatusCode.ShouldBe(401);
        data.Details.ShouldNotBeNull();
        data.Details.ShouldHaveSingleItem();
        data.Details.ShouldContain("Only the bearer authorization scheme is supported.");
    }

    [Fact]
    public static async Task Preferences_Returns_Unauthorized_Json_If_Authorization_Header_Does_Not_Match_A_User()
    {
        // Arrange
        var users = new[]
        {
            new LondonTravelUser() { AlexaToken = null },
            new LondonTravelUser() { AlexaToken = string.Empty },
            new LondonTravelUser() { AlexaToken = "foo" },
            new LondonTravelUser() { AlexaToken = "bar" },
            new LondonTravelUser() { AlexaToken = "bar" },
        };

        string authorizationHeader = "bearer BAR";

        var client = CreateClient(users);

        ApiController target = CreateTarget(client);

        // Act
        var actual = await target.GetPreferences(authorizationHeader, default);

        // Assert
        actual.ShouldNotBeNull();

        actual.Result.ShouldNotBeNull();
        var objectResult = actual.Result.ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(401);
        objectResult.Value.ShouldNotBeNull();

        var data = objectResult.Value.ShouldBeOfType<ErrorResponse>();

        data.Message.ShouldBe("Unauthorized.");
        data.StatusCode.ShouldBe(401);
        data.Details.ShouldNotBeNull();
        data.Details.ShouldBeEmpty();
    }

    [Fact]
    public static async Task Preferences_Returns_Unauthorized_Json_If_Authorization_Header_Does_Not_Match_Alexa_Token_Of_Found_User()
    {
        // Arrange
        var users = new[]
        {
            new LondonTravelUser() { AlexaToken = null },
            new LondonTravelUser() { AlexaToken = string.Empty },
            new LondonTravelUser() { AlexaToken = "foo" },
            new LondonTravelUser() { AlexaToken = "bar" },
            new LondonTravelUser() { AlexaToken = "bar" },
        };

        string authorizationHeader = "bearer BAR";

        var client = CreateClient(users);

        ApiController target = CreateTarget(client);

        // Act
        var actual = await target.GetPreferences(authorizationHeader, default);

        // Assert
        actual.ShouldNotBeNull();

        actual.Result.ShouldNotBeNull();
        var objectResult = actual.Result.ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(401);
        objectResult.Value.ShouldNotBeNull();

        var data = objectResult.Value.ShouldBeOfType<ErrorResponse>();

        data.Message.ShouldBe("Unauthorized.");
        data.StatusCode.ShouldBe(401);
        data.Details.ShouldNotBeNull();
        data.Details.ShouldBeEmpty();
    }

    [Fact]
    public static async Task Preferences_Returns_Correct_User_Preferences_If_Token_Matches_User()
    {
        // Arrange
        var users = new[]
        {
            new LondonTravelUser() { Id = "1", AlexaToken = null, FavoriteLines = Array.Empty<string>() },
            new LondonTravelUser() { Id = "2", AlexaToken = string.Empty, FavoriteLines = Array.Empty<string>() },
            new LondonTravelUser() { Id = "3", AlexaToken = "foo", FavoriteLines = Array.Empty<string>() },
            new LondonTravelUser() { Id = "4", AlexaToken = "bar", FavoriteLines = new[] { "central", "victoria" } },
            new LondonTravelUser() { Id = "5", AlexaToken = "bar", FavoriteLines = new[] { "circle", "waterloo-city" } },
            new LondonTravelUser() { Id = "6", AlexaToken = "BAR", FavoriteLines = new[] { "district" } },
            new LondonTravelUser() { Id = "7", AlexaToken = "bAr", FavoriteLines = new[] { "district" } },
        };

        string authorizationHeader = "BEARER BAR";

        var client = CreateClient(users);

        ApiController target = CreateTarget(client);

        // Act
        var actual = await target.GetPreferences(authorizationHeader, default);

        // Assert
        actual.ShouldNotBeNull();
        actual.Value.ShouldNotBeNull();
        actual.Value.FavoriteLines.ShouldBe(new[] { "district" });
        actual.Value.UserId.ShouldBe("6");
    }

    [Fact]
    public static async Task GetDocumentCount_Returns_Correct_Number_Of_Documents()
    {
        // Arrange
        var mock = new Mock<IAccountService>();

        mock.Setup((p) => p.GetUserCountAsync(false))
            .ReturnsAsync(42);

        ApiController target = CreateTarget(mock.Object);

        // Act
        var actual = await target.GetDocumentCount();

        // Assert
        actual.ShouldNotBeNull();
        actual.Value.ShouldNotBeNull();
        actual.Value.Count.ShouldBe(42);
    }

    /// <summary>
    /// Creates an instance of <see cref="ApiController"/> using mock dependencies.
    /// </summary>
    /// <param name="service">An optional instance of <see cref="IAccountService"/>.</param>
    /// <returns>
    /// The created instance of <see cref="ApiController"/>.
    /// </returns>
    private static ApiController CreateTarget(IAccountService? service = null)
    {
        var httpRequest = new Mock<HttpRequest>();

        httpRequest.Setup((p) => p.Headers).Returns(new HeaderDictionary());

        var httpContext = new Mock<HttpContext>();

        httpContext.Setup((p) => p.Connection).Returns(Mock.Of<ConnectionInfo>());
        httpContext.Setup((p) => p.Request).Returns(httpRequest.Object);

        var actionContext = new ActionContext()
        {
            ActionDescriptor = new ControllerActionDescriptor(),
            HttpContext = httpContext.Object,
            RouteData = new RouteData(),
        };

        var controllerContext = new ControllerContext(actionContext);

        return new ApiController(service ?? Mock.Of<IAccountService>(), Mock.Of<ISiteTelemetry>(), Mock.Of<ILogger<ApiController>>())
        {
            ControllerContext = controllerContext,
        };
    }

    /// <summary>
    /// Creates a mocked implementation of <see cref="IAccountService"/>.
    /// </summary>
    /// <param name="users">The users to store in the mock implementation.</param>
    /// <returns>
    /// The created mocked instance of <see cref="IAccountService"/>.
    /// </returns>
    private static IAccountService CreateClient(IEnumerable<LondonTravelUser> users)
    {
        var mock = new Mock<IDocumentService>();

        mock.Setup((p) => p.GetAsync(It.IsAny<Expression<Func<LondonTravelUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<LondonTravelUser, bool>> a, CancellationToken b) => users.Where(a.Compile()));

        IDocumentService client = mock.Object;
        IMemoryCache cache = Mock.Of<IMemoryCache>();
        ILogger<AccountService> logger = new LoggerFactory().CreateLogger<AccountService>();

        return new AccountService(client, cache, logger);
    }
}
