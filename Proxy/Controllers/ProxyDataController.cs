using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proxy.Models;

namespace Proxy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProxyDataController : ApiController
{
    private readonly ILogger<ProxyDataController> _logger;
    private readonly IServiceProvider _serviceProvider;
    public static HashSet<string> seen = new HashSet<string>();

    public ProxyDataController(ApplicationDbContext context, ILogger<ProxyDataController> logger, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public static bool CheckIfExists(string address, int port)
    {
        if (seen.Contains($"{address}:{port}")) return true;
        
        string checkKey = $"{address}:{port}";
        seen.Add(checkKey);
        
        return false;
    }

    [HttpGet("route")]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string targetUrl)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var proxyPool = context.Proxies.Where(p => p.status == ProxyStatus.Live && p.ping <= 1000 && p.isResidential == true); // Or use a specific criteria to select a proxy
            
            Random r = new();
            
            var proxy = proxyPool.ElementAt(r.Next(0,proxyPool.Count()));

            if (proxy == null)
            {
                return NotFound("Proxy not found");
            }

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy.address, proxy.port),
                UseProxy = true
            };

            using (var httpClient = new HttpClient(httpClientHandler))
            {
                var response = await httpClient.GetAsync(targetUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStreamAsync();
                    var contentType = response.Content.Headers.ContentType.ToString();
                    return new FileStreamResult(content, contentType);
                }
                else
                {
                    return Ok($"{response.StatusCode} Error.");
                    //return StatusCode((int)response.StatusCode, "Failed to retrieve data through proxy");
                }
            }
        }
    }

   [HttpGet("data")]
    public IActionResult Index()
    {
        var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return Ok(context.Proxies);
    }
    
    [HttpGet("export")]
    public IActionResult ExportAll()
    {
        var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var proxies = context.Proxies.Where(p => p.status == ProxyStatus.Live);
        
        var lines = proxies.Select(a => a.ToExportString());
        var builder = new StringBuilder();
        foreach (var line in lines)
            builder.AppendLine(line);

        var content = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
        
        return File(content, "plain/text", "proxies.txt");
    }

    [HttpGet("add")]
    [AllowAnonymous]
    public async Task<IActionResult> AddProxy(string address, int port, string? country, string? isp)
    {
        var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Validate the IP address
        if (!IPAddress.TryParse(address, out var ipAddress))
        {
            return BadRequest("Invalid IP address format.");
        }

        // Validate the port number
        if (port <= 0 || port > 65535)
        {
            return BadRequest("Invalid port number.");
        }

        // Check if the proxy already exists
        if (CheckIfExists(address, port))
        {
            return Unauthorized("Proxy already exists.");
        }

        ProxyModel p = new();

        p.address = address;
        p.port = port;
        p.country = country;
        p.isp = isp;
        p.status = ProxyStatus.Live;
        p.LastScan = DateTime.UtcNow;

        context.Proxies.Add(p);

        _logger.Log(LogLevel.Information, "Added Proxy Via API");

        await context.SaveChangesAsync();

        return Ok($"Proxy {address}:{port.ToString()} added.");
    }
}