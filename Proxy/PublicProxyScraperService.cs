using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Proxy.Controllers;

using Proxy.Models;

namespace Proxy;

public class PublicProxyScraperService : IHostedService, IDisposable
{
    private readonly ILogger<PublicProxyScraperService> _logger;
    private Timer? _timer;
    private readonly IServiceProvider _serviceProvider;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(100);
    private static HashSet<string> proxies = new HashSet<string>();
    private static HttpClient httpClient = new();
    private static Task _backgroundTask;
    private static string UserAgent = "";
    public PublicProxyScraperService(ILogger<PublicProxyScraperService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public async Task StopAsync(CancellationToken token)
    {
        _logger.LogInformation("Public proxy scraper service stopped.");
        _timer?.DisposeAsync();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Proxy Checker service.");

        // Moved the HttpClient to a static or class-level variable to be reused
        // and prevent socket exhaustion issues.
        httpClient ??= new HttpClient();

        // Avoid creating a new CancellationTokenSource linked to the cancellationToken.
        // The cancellationToken provided by the host should be sufficient for your needs.

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = cts.Token;

        // Start the background task
        _backgroundTask = CheckProxiesAsync(cancellationToken);

        // Return a completed task. The actual work is being done in CheckProxiesAsync.
        return Task.CompletedTask;
    }

    public async Task CheckProxiesAsync(CancellationToken token)
    {
        using var UserAgentClient = new HttpClient();
        var doc = new HtmlDocument();
        doc.LoadHtml(await UserAgentClient.GetStringAsync("https://www.useragents.me/#latest-windows-desktop-useragents"));

        var xpath = "//h2[@id='latest-windows-desktop-useragents']/following-sibling::div//tr[td[contains(text(), 'Chrome')]]/td[2]/div/textarea";
        var userAgentNode = doc.DocumentNode.SelectSingleNode(xpath);
        UserAgent = userAgentNode?.InnerText.Trim() ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36";
        var proxyUrls = new List<string>
        {
            "https://rootjazz.com/proxies/proxies.txt",
            "https://raw.githubusercontent.com/ErcinDedeoglu/proxies/main/proxies/http.txt",
            "https://raw.githubusercontent.com/ErcinDedeoglu/proxies/main/proxies/https.txt",
            "https://api.proxyscrape.com/v3/free-proxy-list/get?request=getproxies&protocol=http&format=text&timeout=20000&country=all&ssl=all&anonymity=all",
            "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all",
            "https://api.openproxylist.xyz/http.txt",
			"https://www.proxy-list.download/api/v1/get?type=https",
			"https://www.proxy-list.download/api/v1/get?type=http",
			"https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
            "https://raw.githubusercontent.com/BreakingTechFr/Proxy_Free/main/proxies/all.txt",
            "https://raw.githubusercontent.com/Tsprnay/Proxy-lists/master/proxies/all.txt",
            "https://raw.githubusercontent.com/j0rd1s3rr4n0/api/main/proxy/http.txt",
            "http://proxy11.com/api/proxy.txt?key=NjIwNA.ZbOT2Q.TjOEfpOwriNQ6BUO51Mtk3Drb2k",
            "https://raw.githubusercontent.com/roosterkid/openproxylist/refs/heads/main/HTTPS_RAW.txt"
        };

        while (!token.IsCancellationRequested)
        {
            var proxies = new ConcurrentBag<string>();

            foreach (var url in proxyUrls)
            {
                try
                {
                    var response = await httpClient.GetAsync(url, token);
                    var prxlist = await response.Content.ReadAsStringAsync();
                    var splitprxlist = prxlist.Split(new string[] { "\n" }, StringSplitOptions.None);
                    foreach (var splitprx in splitprxlist)
                    {
                        proxies.Add(splitprx.Trim());
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it appropriately.
                    // In this case, we're ignoring it.
                }
            }

            var distinctProxies = proxies.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            var tasks = distinctProxies.Select(async p =>
            {
                await semaphore.WaitAsync(token);
                try
                {
                    await CheckProxyAsync(p, token);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            await Task.Delay(TimeSpan.FromHours(1), token);
        }
    }
    public async Task CheckProxyAsync(dynamic proxy, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var p = new ProxyModel();

        try
        {
            _logger.LogInformation($"Checking {proxy}");

            if (proxy != null)
            {
                var address = proxy.Split(":")[0];
                var port = Convert.ToInt32(proxy.Split(":")[1]);

                if (!ProxyDataController.CheckIfExists(address, port))
                {
                    p.address = address;
                    p.port = port;

                    using var httpClient = new HttpClient(new HttpClientHandler
                    {
                        UseProxy = true,
                        Proxy = new WebProxy(proxy)
                    });
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
                    httpClient.Timeout = TimeSpan.FromMilliseconds(10000);

                    var blacklistResponse = await httpClient.GetAsync("https://paste.dmca.sh/raw/a8fe2e8f-ff33-4ee3-b324-bc3a20caca7f", cancellationToken);
                    var blacklistResponseContent = await blacklistResponse.Content.ReadAsStringAsync();

                    if (blacklistResponse.IsSuccessStatusCode)
                    {
                        p.isBlacklisted = blacklistResponseContent.Contains("false");
                    }
                    else
                    {
                        p.isBlacklisted = true;
                    }

                    var fraudScoreResponse = await httpClient.GetAsync($"https://scamalytics.com/ip/{address}", cancellationToken);
                    var fraudScoreResponseContent = await fraudScoreResponse.Content.ReadAsStringAsync();

                    if (fraudScoreResponse.IsSuccessStatusCode)
                    {
                        Regex fraudScoreExpression = new Regex(@"<div class=""score"">Fraud Score: [0-9]+");
                        var fraudScoreMatch = fraudScoreExpression.Match(fraudScoreResponseContent);
                        if (fraudScoreMatch.Success)
                        {
                            p.FraudScore = Convert.ToInt32(fraudScoreMatch.Value.Replace(@"<div class=""score"">Fraud Score: ", ""));
                        }
                        else
                        {
                            p.FraudScore = 0;
                        }
                    }
                    else
                    {
                        p.FraudScore = 0;
                    }

                    var timer = Stopwatch.StartNew();
                    var ipInfoResponse = await httpClient.GetAsync($"https://pro.ip-api.com/json/?fields=17035263&key=5mRuJtQYJhXOX8a", cancellationToken);
                    timer.Stop();

                    p.ping = timer.ElapsedMilliseconds;

                    if (ipInfoResponse.IsSuccessStatusCode)
                    {
                        var ipInfoContent = await ipInfoResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(ipInfoContent))
                        {
                            var ipData = JsonSerializer.Deserialize<IPData>(ipInfoContent);
                            if (ipData != null)
                            {
                                p.isResidential = !ipData.Hosting && !ipData.Proxy;
                                p.country = ipData.Country;
                                p.isp = ipData.Isp;
                                p.status = ProxyStatus.Live;

                                
                                var existingProxy = await dbContext.Proxies.FirstOrDefaultAsync(x => x.address == p.address, cancellationToken);

                                if (existingProxy == null)
                                {
                                    dbContext.Proxies.Add(p);
                                    await dbContext.SaveChangesAsync(cancellationToken);
                                    _logger.LogInformation("Added Proxy Via Public Scraper");
                                }
                                else
                                {
                                    _logger.LogInformation("Proxy already exists in the database.");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, do nothing
        }
        catch (Exception)
        {
            //_logger.LogError(ex, "Error while checking proxy");
        }
    }
}
