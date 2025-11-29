using Spectre.Console;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace ProxyChecker
{
    internal class Program
    {
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
        }

        private static int _defaultPort;
        private static string _fileName;
        private static int _timeout;
        private static int _threads;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static ConcurrentQueue<string> ProxyQueue = new ConcurrentQueue<string>();
        static async Task Main(string[] args)
        {
            Initialize(args);

            await LoadProxysAsync(_fileName);
            var tasks = new Task[_threads];
            for (int i = 0; i < _threads; i++)
            {
                tasks[i] = Task.Run(() => CheckProxyAsync(cancellationTokenSource.Token));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            AnsiConsole.Write(new Markup($"[green]All proxies have been checked.[/]\n"));
        }


        static async Task LoadProxysAsync(string filePath)
        {
            var proxies = await File.ReadAllLinesAsync(filePath);
            foreach (var proxy in proxies.Distinct())
            {
                ProxyQueue.Enqueue(proxy + ":" + _defaultPort);
            }
        }

        private static void Initialize(string[] args)
        {
            // Basic argument validation and initialization
            if (args.Length < 4)
            {
                AnsiConsole.Write(new Markup($"[yellow]Usage: ProxyChecker <threads> <timeout> <defaultPort> <fileName>\n[/]"));
                Environment.Exit(1);
            }

            // Validate and parse threads
            if (!int.TryParse(args[0], out _threads) || _threads <= 0)
            {
                AnsiConsole.Write(new Markup($"[red]ERROR: Invalid number of threads.\n[/]"));
                Environment.Exit(1);
            }

            // Validate and parse timeout
            if (!int.TryParse(args[1], out _timeout) || _timeout <= 0)
            {
                AnsiConsole.Write(new Markup($"[red]ERROR: Invalid timeout value.\n[/]"));
                Environment.Exit(1);
            }
            _httpClient.Timeout = TimeSpan.FromMilliseconds(_timeout);

            // Validate and parse default port
            if (!int.TryParse(args[2], out _defaultPort) || _defaultPort < 1 || _defaultPort > 65535)
            {
                AnsiConsole.Write(new Markup($"[red]ERROR: Invalid default port number. Port must be between 1 and 65535.\n[/]"));
                Environment.Exit(1);
            }

            // Validate file name
            _fileName = args[3];
            if (string.IsNullOrWhiteSpace(_fileName))
            {
                Console.WriteLine("ERROR: File name is empty.");
                Environment.Exit(1);
            }

            // Load proxies from file
            if (!File.Exists(_fileName))
            {
                AnsiConsole.Write(new Markup($"[red]ERROR: {_fileName} is missing.\n[/]"));
                Environment.Exit(1);
            }
        }
        public static async Task CheckProxyAsync(CancellationToken cancellationToken)
        {
            while (!ProxyQueue.IsEmpty && !cancellationToken.IsCancellationRequested)
            {
                if (ProxyQueue.TryDequeue(out var proxyAddress))
                {
                    string[] schemes = {
                        "http",
                        "https",
                    };
                    foreach (var scheme in schemes)
                    {
                        try
                        {
                            var httpClientHandler = new HttpClientHandler
                            {
                                Proxy = new WebProxy($"{scheme}://{proxyAddress}", false),
                                UseProxy = true,
                            };
                            using (var httpClient = new HttpClient(httpClientHandler, true))
                            {
                                httpClient.Timeout = TimeSpan.FromMilliseconds(_timeout);
                                var response = await httpClient.GetAsync("https://pro.ip-api.com/json/?fields=17035263&key=5mRuJtQYJhXOX8a");
                                if (response.IsSuccessStatusCode)
                                {
                                    var content = await response.Content.ReadAsStringAsync();
                                    var ipData = JsonSerializer.Deserialize<IPData>(content);
                                    if (!string.IsNullOrEmpty(ipData?.Query))
                                    {
                                        AnsiConsole.Write(new Markup($"[green]Working Proxy: {proxyAddress} - {ipData.Isp} - {ipData.Country} over {scheme.ToUpper()}\n[/]"));
                                        await HandleValidProxy(ipData, proxyAddress, scheme);
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //AnsiConsole.Write(new Markup($"[red]Failed for {scheme.ToUpper()} Proxy {proxyAddress}\n[/]"));
                        }
                    }
                }
            }
        }

        private static async Task HandleValidProxy(IPData ipData, string proxyAddress, string scheme)
        {
            try
            {
                var Response = await _httpClient.GetAsync($"https://proxyenv.net/api/proxydata/add?address={proxyAddress.Split(":")[0]}&port={proxyAddress.Split(":")[1]}&country={ipData.Country}&isp={ipData.Isp}");
                if (Response.IsSuccessStatusCode)
                {
                    AnsiConsole.Write(new Markup($"[green]Successfully reported proxy {proxyAddress} to proxyenv.net[/]"));
                }
                else
                {
                    AnsiConsole.Write(new Markup($"[yellow]Failed to report proxy {proxyAddress} to proxyenv.net. Status code: {Response.StatusCode}[/]"));
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.Write(new Markup($"[red]Error reporting proxy {proxyAddress} to proxyenv.net: {ex.Message}[/]"));
            }
        }
    }
}