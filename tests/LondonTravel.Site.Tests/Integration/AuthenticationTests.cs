// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.LondonTravel.Site.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace MartinCostello.LondonTravel.Site.Integration
{
    /// <summary>
    /// A class containing tests for authentication providers in the website.
    /// </summary>
    public sealed class AuthenticationTests : BrowserIntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationTests"/> class.
        /// </summary>
        /// <param name="fixture">The fixture to use.</param>
        /// <param name="outputHelper">The <see cref="ITestOutputHelper"/> to use.</param>
        public AuthenticationTests(HttpServerFixture fixture, ITestOutputHelper outputHelper)
            : base(fixture, outputHelper)
        {
            Fixture.Services!.GetRequiredService<InMemoryDocumentStore>().Clear();
        }

        [Theory]
        [InlineData("amazon", "John")]
        [InlineData("apple", "")]
        [InlineData("facebook", "John")]
        [InlineData("github", "john-smith")]
        [InlineData("google", "John")]
        [InlineData("microsoft", "John")]
        [InlineData("twitter", "@JohnSmith")]
        public async Task Can_Sign_In_And_Out_With_External_Provider(string provider, string expected)
        {
            // Arrange
            await AtPageAsync<HomePage>(
                "chromium",
                async (page) =>
                {
                    page = await page
                        .SignInAsync()
                        .ThenAsync((p) => p.SignInWithProviderAsync(provider));

                    // Assert
                    await page.IsAuthenticatedAsync().ShouldBeTrue();
                    await page.UserNameAsync().ShouldBe(expected);

                    // Act
                    page = await page.SignOutAsync();

                    // Assert
                    await page.IsAuthenticatedAsync().ShouldBeFalse();
                });
        }

        [Theory]
        [ClassData(typeof(BrowsersTestData))]
        public async Task Can_Delete_Account(string browserType)
        {
            // Arrange
            await AtPageAsync<HomePage>(
                browserType,
                async (homepage) =>
                {
                    ManagePage page = await homepage
                        .SignInAsync()
                        .ThenAsync((p) => p.SignInWithAmazonAsync())
                        .ThenAsync((p) => p.ManageAsync());

                    // Act
                    await page.DeleteAccountAsync()
                              .ThenAsync((p) => p.CloseAsync());

                    // Assert
                    await page.IsAuthenticatedAsync().ShouldBeTrue();

                    // Act
                    await page.DeleteAccountAsync()
                              .ThenAsync((p) => p.ConfirmAsync());

                    // Assert
                    await page.IsAuthenticatedAsync().ShouldBeFalse();
                });
        }

        [Theory]
        [ClassData(typeof(BrowsersTestData))]
        public async Task Can_Link_Accounts(string browserType)
        {
            // Arrange
            await AtPageAsync<HomePage>(
                browserType,
                async (homepage) =>
                {
                    ManagePage page = await homepage
                        .SignInAsync()
                        .ThenAsync((p) => p.SignInWithAmazonAsync())
                        .ThenAsync((p) => p.ManageAsync());

                    // Assert
                    IReadOnlyList<LinkedAccount> accounts = await page.LinkedAccountsAsync();

                    accounts.Count.ShouldBe(1);
                    await accounts[0].NameAsync().ShouldBe("Amazon");

                    // Act
                    page = await page.SignInWithGoogleAsync();

                    // Assert
                    accounts = await page.LinkedAccountsAsync();

                    accounts.Count.ShouldBe(2);
                    await accounts[0].NameAsync().ShouldBe("Amazon");
                    await accounts[^1].NameAsync().ShouldBe("Google");

                    // Act
                    page = await accounts[0].RemoveAsync();

                    // Assert
                    accounts = await page.LinkedAccountsAsync();

                    accounts.Count.ShouldBe(1);
                    await accounts[0].NameAsync().ShouldBe("Google");
                });
        }
    }
}
