using Renci.SshNet;

namespace Proxy;

public class PrivateProxyScraperService : IHostedService, IDisposable
{
    private readonly ILogger<PrivateProxyScraperService> _logger;
    private Timer? _timer;
    private static HttpClient _httpClient = new();
    private static Task _backgroundTask;

    public PrivateProxyScraperService(ILogger<PrivateProxyScraperService> logger)
    {
        _logger = logger;
    }
    private static Random random = new Random();

    public static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Proxy Checker service.");

        // Moved the HttpClient to a static or class-level variable to be reused
        // and prevent socket exhaustion issues.
        _httpClient ??= new HttpClient();

        // Avoid creating a new CancellationTokenSource linked to the cancellationToken.
        // The cancellationToken provided by the host should be sufficient for your needs.

        // Start the background task
        _backgroundTask = CheckProxiesAsync(cancellationToken);

        // Return a completed task. The actual work is being done in CheckProxiesAsync.
        return Task.CompletedTask;
    }

    public async Task CheckProxiesAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _logger.LogInformation("Starting Private proxy scraper service.");

            try
            {
                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 80.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 80 -o 80.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 443.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 443 -o 443.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 8081.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 8081 -o 8081.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 8080.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 8080 -o 8080.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 3128.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 3128 -o 3128.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 1337.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 1337 -o 1337.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 666.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 666 -o 666.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 999.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 999 -o 999.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 69.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 69 -o 69.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);


                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 8888.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 8888 -o 8888.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 4444.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 4444 -o 4444.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);
			
                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 8008.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 8008 -o 8008.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 8443.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 8443 -o 8443.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port"))
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"rm 777.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} zmap -p 777 -o 777.txt");
                }

                await Task.Delay(TimeSpan.FromMinutes(35), token);

                using (var client = new SshClient("ip", "root", "port")
                {
                    client.Connect();
                    client.RunCommand($"pkill screen");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 80 80.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 443 443.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 1337 1337.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 666 666.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 69 69.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 8080 8080.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 8081 8081.txt");
                    client.RunCommand($"screen -dmS {RandomString(7)} dotnet ProxyChecker.dll 10000 10000 3128 3128.txt");
                }

                await Task.Delay(TimeSpan.FromDays(5), token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"PrivateProxyScraperService.cs: {e.Message.ToString()}");
                await Task.Delay(TimeSpan.FromDays(1), token);
            }
        };
    }

    public async Task StopAsync(CancellationToken token)
    {
        _logger.LogInformation("Private scraper service stopped.");
        _timer?.DisposeAsync();
    }
}
