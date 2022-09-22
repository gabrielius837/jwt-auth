
namespace App.UnitTests;

public class ErrorHandlingMiddleware_Tests
{
    [Fact]
    public async Task Invoke_ReturnsErrorResponse_WhenErrorOccurs()
    {
        // arrange
        const string msg = "Error";
        Func<string> callback = () => throw new Exception(msg);
        using var host = await GetHost(callback);

        // act
        var response = await host.GetTestClient().GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<ErrorResponse>(body);

        // assert
        Assert.Equal(HttpStatusCode.InternalServerError, response?.StatusCode);
        Assert.Equal(msg, error?.Message);
        Assert.Equal(500, error?.StatusCode);
        Assert.NotNull(error?.TraceId);
    }

    [Fact]
    public async Task Invoke_ReturnsResponse_WhenEverythingIsOkay()
    {
        // arrange
        const string msg = "random msg";
        Func<string> callback = () => msg;
        using var host = await GetHost(callback);

        // act
        var response = await host.GetTestClient().GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        // assert
        Assert.Equal(HttpStatusCode.OK, response?.StatusCode);
        Assert.Equal(body, msg);
    }

    public static async Task<IHost> GetHost<T>(Func<T> callback)
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
                        app.UseErrorHandling();
                        app.UseRouting();
                        app.UseEndpoints(options =>
                        {
                            options.MapGet("/", callback);
                        });
                    });
            })
            .StartAsync();

        return host;
    }
}