using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using IdentityManagerUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proxy.Models;
using X.PagedList;
using X.PagedList.Extensions;

namespace Proxy.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly Proxy.ApplicationDbContext _context;
    private IQueryable<ProxyModel> proxies;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly Dictionary<string, string> _roles;
    private readonly Dictionary<string, string> _claimTypes;
    
    public HomeController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogger<HomeController> logger, Proxy.ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _context = context;

        _roles = roleManager.Roles.ToDictionary(r => r.Id, r => r.Name);
        var fldInfo = typeof(ClaimTypes).GetFields(BindingFlags.Static | BindingFlags.Public);
        _claimTypes = fldInfo.ToDictionary(i => i.Name, i => (string)i.GetValue(null));
    }
    
    [Authorize(Roles = "Admin")]
    public IActionResult Users()
    {
        ViewBag.Roles = _roles;
        ViewBag.ClaimTypes = _claimTypes.Keys.OrderBy(s => s);
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Roles()
    {
        ViewBag.ClaimTypes = _claimTypes.Keys.OrderBy(s => s);
        return View();
    }
    
    //[Authorize(Roles = "Premium")]
    public IActionResult Index(string sortOrder, string currentFilter, string searchString, int? page, string exportCurrent = "false")
    {
        ViewBag.NameSortParam = sortOrder == "Name" ? "name_desc" : "Name";
        ViewBag.SpeedSortParam = sortOrder == "Speed" ? "speed_desc" : "Speed";
        ViewBag.CountrySortParam = sortOrder == "Country" ? "country_desc" : "Country";
        ViewBag.BlackListedSortParam = sortOrder == "Blacklisted" ? "blacklist_desc" : "Blacklisted";
        ViewBag.ResidentialSortParam = sortOrder == "Residential" ? "residential_desc" : "Residential";
        ViewBag.FraudScoreSortParam = sortOrder == "Fraud Score" ? "fraudscore_desc" : "Fraud Score";
        ViewBag.CurrentSort = sortOrder;
        

        if (searchString != null)
        {
            page = 1;
        }
        else
        {
            searchString = currentFilter;
        }

        ViewBag.CurrentFilter = searchString;
        
        proxies = from s in _context.Proxies
            where s.status != ProxyStatus.Dead
            select s;
        
        if (!String.IsNullOrEmpty(searchString))
        {
            proxies = proxies.Where(s => s.isp.ToLower().Contains(searchString.ToLower()));
        }
        
        switch (sortOrder)
        {
            case "Name":
                proxies = proxies.OrderBy(s => s.country);
                break;
            case "name_desc":
                proxies = proxies.OrderByDescending(s => s.country);
                break;
            case "Speed":
                proxies = proxies.OrderBy(s => s.ping);
                break;
            case "Country":
                proxies = proxies.OrderBy(s => s.country);
                break;
            case "country_desc":
                proxies = proxies.OrderByDescending(s => s.country);
                break;
            case "Blacklisted":
                proxies = proxies.OrderBy(s => s.isBlacklisted);
                break;
            case "blacklist_desc":
                proxies = proxies.OrderByDescending(s => s.isBlacklisted);
                break;
            case "Residential":
                proxies = proxies.OrderBy(s => s.isResidential);
                break;
            case "residential_desc":
                proxies = proxies.OrderByDescending(s => s.isResidential);
                break;
            case "Fraud Score":
                proxies = proxies.OrderBy(s => s.FraudScore);
                break;
            case "fraudscore_desc":
                proxies = proxies.OrderByDescending(s => s.FraudScore);
                break;
            case "speed_desc":
                proxies = proxies.OrderByDescending(s => s.ping);
                break;
            default:
                proxies = proxies.OrderBy(s => s.Id);
                break;
        }

        int pageSize = 25;
        int pageNumber = (page ?? 1);

        var viewPage = proxies.ToPagedList(pageNumber, pageSize);
        
        if (exportCurrent.Equals("true"))
        {
            var builder = new StringBuilder();
            
            foreach (var proxy in viewPage)
                builder.AppendLine(proxy.ToExportString());

            var content = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
        
            return File(content, "plain/text", "proxies.txt");
        }
        
        if (exportCurrent.Equals("all"))
        {
            var builder = new StringBuilder();
            
            foreach (var proxy in _context.Proxies.Where(p => p.status != ProxyStatus.Dead))
                builder.AppendLine(proxy.ToExportString());

            var content = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
        
            return File(content, "plain/text", "proxies.txt");
        }
         
        return View(viewPage);
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}