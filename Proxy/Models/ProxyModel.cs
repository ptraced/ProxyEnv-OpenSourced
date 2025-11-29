using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proxy.Models;

public enum ProxyStatus
{
    Dead = 0,
    Live = 1
}

public class ProxyModel : IDisposable
{
    [Key] public long Id { get; set; }
    public string address { get; set; }
    public string? username { get; set; }
    public string? password { get; set; }
    public int port { get; set; }
    public long ping { get; set; } = 3000;
    public string? isp { get; set; }
    public string? country { get; set; }
    public bool? isBlacklisted { get; set; } = false;
    public bool? isResidential { get; set; } = false;
    public int? FraudScore { get; set; } = 0;
    public ProxyStatus status { get; set; }
    public DateTime DeadSince { get; set; }
    public DateTime LastScan { get; set; }

    [NotMapped]
    private bool disposed;

    public string ToExportString()
    {
        if (username?.Length > 0 && password?.Length > 0)
        {
            return $"{address}:{port}:{username}:{password}";
        }

        return $"{address}:{port}";
    }

    public ProxyModel() {
    }
    
    ~ProxyModel() {
        Dispose(false);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing) {
        if (disposed) {
            return;
        }

        if (disposing) {
            // Dispose managed objects

            Id = 0;
            address = null;
            username = null;
            password = null;
            port = 0;
            ping = 0;
            isp = null;
            country = null;
            isBlacklisted = false;
            isResidential = false;
            FraudScore = 0;
            status = 0;
        }
        
        // Dispose unmanaged objects
        disposed = true;
    }
}