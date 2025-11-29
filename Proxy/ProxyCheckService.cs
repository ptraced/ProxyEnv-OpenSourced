using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Proxy.Models;
using ProxyStatus = Proxy.Models.ProxyStatus;

namespace Proxy;

public class IPData
{
    [JsonPropertyName("as")]
    public string? As { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("isp")]
    public string? Isp { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("org")]
    public string? Org { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("regionName")]
    public string? RegionName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("proxy")]
    public bool Proxy { get; set; }

    [JsonPropertyName("hosting")]
    public bool Hosting { get; set; }

    [JsonPropertyName("mobile")]
    public bool Mobile { get; set; }
}

public class ProxyCheckService : IDisposable
{
    private readonly ILogger<ProxyCheckService> _logger;
    private Timer? _timer;
    private readonly IServiceProvider _serviceProvider;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(300);
    // Class-level variables
    private HttpClient _httpClient;
    private Task _backgroundTask;

    public ProxyCheckService(ILogger<ProxyCheckService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

	public Task InitializeAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Starting Proxy Checker service.");

		_httpClient ??= new HttpClient();

		// Delay background task execution to improve startup time
		Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(t =>
		{
			_backgroundTask = CheckProxiesAsync(cancellationToken);
		}, TaskContinuationOptions.OnlyOnRanToCompletion);

		return Task.CompletedTask;
	}

	private async Task CheckProxiesAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    await using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    if (context.Proxies != null)
                    {
                        var proxies = context.Proxies.Distinct().ToList();

                        var tasks = proxies.Select(proxy => CheckProxyAsync(proxy.Id, token)).ToList();

                        foreach (var task in tasks)
                        {
                            await semaphore.WaitAsync(token);

                            _ = task.ContinueWith(async t =>
                            {
                                semaphore.Release();
                            }, token);
                        }

                        await Task.WhenAll(tasks);
                    }
                }
            }
            catch (Exception)
            {

            }

            await Task.Delay(TimeSpan.FromMinutes(30), token);
        }
    }


    public async Task StopAsync(CancellationToken token)
    {
        _logger.LogInformation("Proxy Checker service is stopping.");

        if (_backgroundTask != null)
        {
            token.ThrowIfCancellationRequested();

            await Task.WhenAny(_backgroundTask, Task.Delay(Timeout.Infinite, token));
        }

        _logger.LogInformation("Proxy Checker service stopped.");
    }

	public async Task CheckIPDataAsync(ProxyModel proxy, HttpClient httpClient, CancellationToken cancellationToken)
	{
		var stopwatch = new Stopwatch();
		stopwatch.Start();
		var ipDataResponse = await httpClient.GetAsync($"https://pro.ip-api.com/json/?fields=17035263&key=5mRuJtQYJhXOX8a", cancellationToken);
		stopwatch.Stop();
		proxy.ping = stopwatch.ElapsedMilliseconds;

		if (ipDataResponse.IsSuccessStatusCode)
		{
			var ipDataContent = await ipDataResponse.Content.ReadAsStringAsync(cancellationToken);
			if (!string.IsNullOrEmpty(ipDataContent))
			{
				var ipData = JsonSerializer.Deserialize<IPData>(ipDataContent);
				if (ipData != null)
				{
					proxy.isResidential = !ipData.Hosting && !ipData.Proxy;
					proxy.country = ipData.Country;
					proxy.isp = ipData.Isp;
					proxy.status = ProxyStatus.Live;
				}
			}
		}
		else
		{
			MarkProxyAsDead(proxy);
		}
	}

	public async Task CheckBlacklistAsync(ProxyModel proxy, HttpClient httpClient, CancellationToken cancellationToken)
	{
		try
		{
			var blackListResponse = await httpClient.GetAsync("https://paste.dmca.sh/raw/a8fe2e8f-ff33-4ee3-b324-bc3a20caca7f", cancellationToken);
			if (blackListResponse.IsSuccessStatusCode)
			{
				var blackListContent = await blackListResponse.Content.ReadAsStringAsync(cancellationToken);
				proxy.isBlacklisted = (!blackListContent.Contains("false"));
			}
			else
			{
				proxy.isBlacklisted = true;
			}
		}
		catch (Exception)
		{
			proxy.isBlacklisted = false;
		}
	}

	public async Task CheckFraudScoreAsync(ProxyModel proxy, HttpClient httpClient, CancellationToken cancellationToken)
	{
		try
		{
			var fraudScoreResponse = await httpClient.GetAsync($"https://scamalytics.com/ip/{proxy.address}", cancellationToken);
			if (fraudScoreResponse.IsSuccessStatusCode)
			{
				var fraudScoreContent = await fraudScoreResponse.Content.ReadAsStringAsync(cancellationToken);
				var fraudScoreExpression = new Regex(@"<div class=""score"">Fraud Score: [0-9]+");
				var fraudScoreMatch = fraudScoreExpression.Match(fraudScoreContent);
				if (fraudScoreMatch.Success)
				{
					proxy.FraudScore = Convert.ToInt32(fraudScoreMatch.Value.Replace(@"<div class=""score"">Fraud Score: ", ""));
				}
			}
			else
			{
				proxy.FraudScore = 0;
			}
		}
		catch (Exception ex)
		{
			proxy.FraudScore = 0;
		}
	}

    public static async Task CheckSnapchatAccessAsync(ProxyModel proxy, HttpClient httpClient, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync($"https://www.snapchat.com/@vlad", cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                proxy.status = ProxyStatus.Live;
            }
            else
            {
                proxy.status = ProxyStatus.Dead;
            }
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                proxy.status = ProxyStatus.Live;
            }
            else
            {
                proxy.status = ProxyStatus.Dead;
            }
        }
    }

    private void MarkProxyAsDead(ProxyModel proxy)
	{
		if (proxy.status != ProxyStatus.Dead)
		{
			proxy.DeadSince = DateTime.UtcNow;
		}
		proxy.status = ProxyStatus.Dead;
	}

	public async Task CheckProxyAsync(object? proxyId, CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken);
		try
		{
			using var scope = _serviceProvider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			if (context.Proxies == null)
			{
				throw new InvalidOperationException("Proxies collection is not available.");
			}

			var proxy = await context.Proxies.FindAsync(new object[] { proxyId }, cancellationToken);
			if (proxy == null)
			{
				throw new KeyNotFoundException($"Proxy with ID {proxyId} not found.");
			}

			using var httpClient = CreateHttpClientForProxy(proxy);
			try
			{
				await CheckIPDataAsync(proxy, httpClient, cancellationToken);
			}catch(Exception)
			{
				Console.WriteLine("Failed to check IPData");
			}

			try
			{
				await CheckBlacklistAsync(proxy, httpClient, cancellationToken);
			}catch(Exception)
			{
				Console.WriteLine("Failed to check Blacklist");
			}

			try
			{
				await CheckSnapchatAccessAsync(proxy, httpClient, cancellationToken);
			}catch(Exception)
			{
				MarkProxyAsDead(proxy);
				Console.WriteLine("Failed to check snapchat access");
			}

			proxy.LastScan = DateTime.UtcNow;
			context.Proxies.Update(proxy);

			await RemoveDeadProxies(context, cancellationToken);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error occurred while checking proxy: {ex.Message}");
		}
		finally
		{
			semaphore.Release();
		}
	}


	private HttpClient CreateHttpClientForProxy(ProxyModel proxy)
	{
		var hasCredentials = !string.IsNullOrEmpty(proxy.username) && !string.IsNullOrEmpty(proxy.password);
		var handler = new HttpClientHandler
		{
			UseProxy = true,
			Proxy = new WebProxy($"{proxy.address}:{proxy.port}", true, null,
					hasCredentials ? new NetworkCredential(proxy.username, proxy.password) : null)
		};

		var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(120) };
		return httpClient;
	}

	private async Task RemoveDeadProxies(ApplicationDbContext context, CancellationToken cancellationToken)
	{
		var deadProxies = context.Proxies.Where(p => p.status == ProxyStatus.Dead && p.DeadSince.AddDays(3) <= DateTime.UtcNow).ToList();

		context.Proxies.RemoveRange(deadProxies);
		await context.SaveChangesAsync(cancellationToken);
	}
}
