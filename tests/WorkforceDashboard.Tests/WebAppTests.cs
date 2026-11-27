using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WorkforceDashboard.Tests;

public class WebAppTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Dashboard_page_renders_with_security_headers()
    {
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("All units", html);
        Assert.Contains("id=\"dashboard-data\"", html);
        Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Bad_link_shows_the_default_view_with_a_warning()
    {
        var html = await _client.GetStringAsync("/?scope=%3Cscript%3E");
        Assert.Contains("Showing the default view instead", html);
        Assert.DoesNotContain("<script>\"", html);
    }

    [Fact]
    public async Task Api_returns_the_selected_view()
    {
        var json = await _client.GetStringAsync("/api/dashboard?scope=EPW&year=2022&cadre=executive");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("EPW", root.GetProperty("scope").GetString());
        Assert.Equal(2022, root.GetProperty("year").GetInt32());
        Assert.Equal("Executive", root.GetProperty("cadre").GetString());
        Assert.True(root.GetProperty("kpis").GetProperty("manpower").GetInt32() > 0);
    }

    [Theory]
    [InlineData("/api/dashboard?scope=nowhere")]
    [InlineData("/api/dashboard?cadre=managers")]
    [InlineData("/api/dashboard?year=1999")]
    public async Task Api_rejects_bad_queries_with_problem_details(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_reports_the_data_source()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"Healthy\"", body);
        Assert.Contains("Sample data", body);
    }

    [Fact]
    public async Task Units_endpoint_lists_groups()
    {
        var json = await _client.GetStringAsync("/api/units");
        Assert.Contains("Integrated plants", json);
    }
}
