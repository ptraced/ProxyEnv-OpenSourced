using IdentityManagerUI.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Proxy;

public class Startup   
{  
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)  
    {
    }  
    
    public void ConfigureServices(IServiceCollection services)  
    {  
  
    }  
    
    public Startup()      
    {   
        var connectionString = "server=ip;port=3306;user=user;password=pass;database=database;";
        var serverVersion = new MariaDbServerVersion(ServerVersion.AutoDetect("server=ip;port=3306;user=user;password=pass;database=database;"));
        
        Program.builder.Services.AddDbContext<ApplicationDbContext>(
            dbContextOptions => dbContextOptions
                .UseMySql(connectionString, serverVersion, mySqlOptions => mySqlOptions
			        .EnableRetryOnFailure(
				        maxRetryCount: 5,
				        maxRetryDelay: TimeSpan.FromSeconds(30),
				        errorNumbersToAdd: null
			        )
                   
		        )
                .LogTo(Console.WriteLine, LogLevel.Error)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
               
        );

		Program.builder.Services.AddRazorPages();

		Program.builder.Services.AddDatabaseDeveloperPageExceptionFilter();

		Program.builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
		{
			options.SignIn.RequireConfirmedAccount = false;
			options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
			options.Lockout.MaxFailedAccessAttempts = 4;
		})
	    .AddRoles<ApplicationRole>()
	    .AddEntityFrameworkStores<ApplicationDbContext>();

		Program.builder.Services.AddControllersWithViews();

        Program.builder.WebHost.ConfigureKestrel((context, options) =>
        {
            options.ListenLocalhost(777, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1;
            });
        });

        Program.builder.Services.AddEndpointsApiExplorer();
		Program.builder.Services.AddAuthentication();
		Program.builder.Services.AddSingleton<ProxyCheckService>();
		Program.builder.Services.AddHostedService<PublicProxyScraperService>();
        Program.builder.Services.AddHostedService<PrivateProxyScraperService>();
		Program.builder.Services.TryAddScoped<SignInManager<ApplicationUser>>();
		Program.builder.Services.TryAddScoped<UserManager<ApplicationUser>>();

		new Startup(Program.builder, new LoggerFactory());
    }  
  
    public Startup(WebApplicationBuilder appenv, ILoggerFactory loggerFactory)
    {
        var app = appenv.Build().MigrateDatabase<ApplicationDbContext>();

		var initService = app.Services.GetService<ProxyCheckService>();
		if (initService != null)
		{
			var task = Task.Run(async () => await initService.InitializeAsync(new CancellationToken()));
		}

		//loggerFactory.AddFile($@"{Directory.GetCurrentDirectory()}/log.txt");

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();
            app.UseExceptionHandler("/Home/Error");
    
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    
            app.UseHsts();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapRazorPages();

        app.Run();
    }  
}
