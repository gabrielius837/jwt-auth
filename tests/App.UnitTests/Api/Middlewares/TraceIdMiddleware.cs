
namespace App.UnitTests;

public class TraceIdMiddleware_Tests
{
    [Fact]
    public async Task Invoke_ReturnsGuidInTraceHeader_WhenValueIsNotDefined()
    {
        // arrange
        using var host = await GetHost();

        // act
        var response = await host.GetTestClient().GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();
        var header = response.Headers
            .FirstOrDefault(header => header.Key == TraceIdMiddleware.TRACE_ID_HEADER)
            .Value?.FirstOrDefault();
        var isGuid = Guid.TryParse(body, out Guid guid);

        // assert
        Assert.Equal(HttpStatusCode.OK, response?.StatusCode);
        Assert.Equal(body, header);
        Assert.True(isGuid);
    }

    public async Task Invoke_ReturnsValueInTraceHeader_WhenItsSetInRequest()
    {
        // arrange
        using var host = await GetHost();

        // act
        const string traceHeader = "trace-test";
        var msg = new HttpRequestMessage(HttpMethod.Get, "/");
        msg.Headers.Add(TraceIdMiddleware.TRACE_ID_HEADER, traceHeader);
        var response = await host.GetTestClient().SendAsync(msg);
        var body = await response.Content.ReadAsStringAsync();
        var header = response.Headers
            .FirstOrDefault(header => header.Key == TraceIdMiddleware.TRACE_ID_HEADER)
            .Value?.FirstOrDefault();

        // assert
        Assert.Equal(HttpStatusCode.OK, response?.StatusCode);
        Assert.Equal(traceHeader, header);
        Assert.Equal(traceHeader, body);
    }

    public static async Task<IHost> GetHost()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                    })
                    .Configure(app =>
                    {
                        app.UseTraceId();
                        app.UseRouting();
                        app.UseEndpoints(options =>
                        {
                            options.MapGet("/", async context =>
                            {
                                var id = context.TraceIdentifier;
                                await context.Response.WriteAsync(id);
                            });
                        });
                    });
            })
            .StartAsync();

        return host;
    }
}