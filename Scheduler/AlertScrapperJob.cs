using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MangaAlert.Repositories;
using Microsoft.Extensions.Hosting;

namespace MangaAlert.Scheduler
{
  public class AlertScrapperJob: IHostedService, IDisposable
  {
    private Timer _timer;
    private readonly IAlertRepository _alertRepository;
    private static readonly HttpClient Client = new HttpClient();

    private bool IsValidUrl(string url)
    {
      return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
             && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private static int GetLatestReleaseFromMangakakalot(string responseString)
    {

    }

    private static int GetLatestReleaseFromPahe(string responseString)
    {

    }

    private static int GetLatestReleaseFromMangaHub(string responseString)
    {

    }

    private static int GetLatestReleaseFromToomics(string responseString)
    {

    }

    public AlertScrapperJob(IAlertRepository alertRepository)
    {
      this._alertRepository = alertRepository;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
      // Runs method to scrap all unique urls every hour
      _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
      return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
      var allUniqueUrls = (await _alertRepository.GetAllUniqueAlertsByUrl());

      foreach (var url in allUniqueUrls) {
        Console.WriteLine(url);

        try {
          if (IsValidUrl(url)) {
            var response = await Client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            int latestRelease;
            var domainNameOfUrl = new Uri(url).Host;

;            switch (domainNameOfUrl) {
              case "www.mangakakalot.com":
                latestRelease = GetLatestReleaseFromMangakakalot(responseString);
                await _alertRepository.BulkUpdateAlert(url, latestRelease);
                break;
              case "www.pahe.win":
                latestRelease = GetLatestReleaseFromPahe(responseString);
                await _alertRepository.BulkUpdateAlert(url, latestRelease);
                break;
              case "www.mangahub.io":
                latestRelease = GetLatestReleaseFromMangaHub(responseString);
                await _alertRepository.BulkUpdateAlert(url, latestRelease);
                break;
              case "www.toomics.com":
                latestRelease = GetLatestReleaseFromToomics(responseString);
                await _alertRepository.BulkUpdateAlert(url, latestRelease);
                break;
              case "www.readmanganato.com":
                latestRelease = GetLatestReleaseFromMangakakalot(responseString);
                await _alertRepository.BulkUpdateAlert(url, latestRelease);
                break;
            }
          }
        }
        catch (InvalidCastException e) {
          Console.WriteLine($"error: {e}");
        }
      }
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
