using System.Net;
using System.Net.Http.Json;
using ArchitectureStandardsInitExample.Api.Diagnostics;
using ArchitectureStandardsInitExample.Api.Tests.Infrastructure;
using ArchitectureStandardsInitExample.Contracts;

namespace ArchitectureStandardsInitExample.Api.Tests.Diagnostics;

/// <summary>
/// The list endpoint's clamping and the uniform error shape — the two rules from
/// SERVICE-API-PATTERNS that a diff can silently break, and that cost an outage
/// and a client rewrite respectively when they do.
/// </summary>
public sealed class BootRecordEndpointTests : IClassFixture<ZeroCredentialFactory>
{
    private readonly ZeroCredentialFactory _factory;

    public BootRecordEndpointTests(ZeroCredentialFactory factory) => _factory = factory;

    [Fact]
    public async Task List_clamps_an_absurd_limit_to_the_maximum_page_size()
    {
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResponse<BootRecordResponse>>(
            "/api/boots?limit=2000000",
            TestContext.Current.CancellationToken);

        // An unclamped limit is a one-line outage, and the response has to report
        // the limit that was applied rather than the one that was asked for —
        // otherwise a client pages forever against a number it never got.
        Assert.NotNull(page);
        Assert.Equal(BootRecordEndpoints.MaxPageSize, page.Limit);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public async Task List_clamps_a_nonsensical_page_to_the_first_one(int requested, int expected)
    {
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResponse<BootRecordResponse>>(
            $"/api/boots?page={requested}",
            TestContext.Current.CancellationToken);

        Assert.NotNull(page);
        Assert.Equal(expected, page.Page);
    }

    [Fact]
    public async Task Unknown_id_returns_404_in_the_estate_wide_error_shape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/boots/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>(TestContext.Current.CancellationToken);

        // A client branches on the code and displays the message. Both have to be
        // there, and the code has to be the stable one every endpoint uses.
        Assert.NotNull(error);
        Assert.Equal("not_found", error.Error);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }
}
