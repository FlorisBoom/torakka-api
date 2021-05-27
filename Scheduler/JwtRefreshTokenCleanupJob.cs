using System;
using System.Threading;
using System.Threading.Tasks;
using MangaAlert.Services;
using Microsoft.Extensions.Hosting;

namespace MangaAlert.Scheduler
{
  public class JwtRefreshTokenCleanupService: IHostedService, IDisposable
  {
    private Timer _timer;
    private readonly IJwtManagerService _jwtManagerService;

    public JwtRefreshTokenCleanupService(IJwtManagerService jwtManagerService)
    {
      _jwtManagerService = jwtManagerService;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
      // Remove expired refresh tokens from cache every hour
      _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(60));
      return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
      _jwtManagerService.RemoveExpiredRefreshTokens(DateTime.Now);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
      _timer?.Change(Timeout.Infinite, 0);
      return Task.CompletedTask;
    }

    public void Dispose()
    {
      _timer?.Dispose();
    }
  }
}
