public class Program
{
    public static WebApplicationBuilder builder = null;
    
    public static void Main(string[] args)  
    {
        builder = WebApplication.CreateBuilder(args);
        
        new WebHostBuilder()
            .UseContentRoot(Directory.GetCurrentDirectory())  
            .UseStartup<Startup>()  
            .Build();
    }  
}