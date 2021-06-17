using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAlert.Repositories;
using Microsoft.Extensions.Hosting;

namespace MangaAlert.Scheduler
{
  public class AlertScrapperJob: IHostedService, IDisposable
  {
    private Timer _timer;
    private readonly ITrackerRepository _trackerRepository;
    private static readonly HtmlWeb Web = new HtmlWeb();

    private bool IsValidUrl(string url)
    {
      return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
             && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private static int GetLatestReleaseFromMangakakalot(string url)
    {
      var doc = Web.Load(url);
      var nodes = doc.DocumentNode.SelectSingleNode("//div[@class='chapter-list']/div[1]")
        .Descendants("span")
        .Select(span => span.Descendants("a")
          .Select(a => a.InnerText)
          .ToList())
        .ToList();

      var firstValue = nodes.First().First();

      return int.Parse(string.Join("", new Regex("[0-9]").Matches(firstValue)));
    }

    private static int GetLatestReleaseFromManganato(string url)
    {
      var doc = Web.Load(url);
      var nodes = doc.DocumentNode.SelectSingleNode("//ul[@class='row-content-chapter']")
        .Descendants("li")
        .Select(li => li.Descendants("a")
          .Select(a => a.InnerText)
          .ToList())
        .ToList();

      var firstValue = nodes.First().First();

      return int.Parse(string.Join("", new Regex("[0-9]").Matches(firstValue)));
    }

    private static int GetLatestReleaseFromPahe(string url)
    {
      var doc = Web.Load(url);
      var nodes = doc.DocumentNode.Descendants("title").FirstOrDefault();

      var firstValue = nodes.InnerText;

      return int.Parse(string.Join("", new Regex("[0-9]").Matches(firstValue))) - 100;
    }

    private static int GetLatestReleaseFromMangaHub(string url)
    {
      var doc = Web.Load(url);
      var nodes = doc.DocumentNode.SelectSingleNode("//ul[@class='MWqeC list-group']/li[1]/a/span/span");

      var firstValue = nodes.InnerText;

      return int.Parse(string.Join("", new Regex("[0-9]").Matches(firstValue)));
    }

    private static int GetLatestReleaseFromToomics(string url)
    {
      var doc = Web.Load(url);
      var nodes = doc.DocumentNode.SelectSingleNode("//ol[@class='list-ep']/li[@class='normal_ep own'][last()]/a/div[2]/span");

      var firstValue = nodes.InnerText;

      return int.Parse(string.Join("", new Regex("[0-9]").Matches(firstValue)));
    }

    public AlertScrapperJob(ITrackerRepository trackerRepository)
    {
      this._trackerRepository = trackerRepository;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
      // Runs method to scrap all unique urls every 3 hours
      // _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(3));
      return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
      var allUniqueUrls = (await _trackerRepository.GetAllUniqueTrackersByUrl());

      foreach (var url in allUniqueUrls) {
        try {
          if (!IsValidUrl(url)) continue;
          var nextRelease = (await _trackerRepository.GetNextReleaseForUrl(url));

          if (
            (nextRelease.Any() &&
             ((DayOfWeek)Enum.Parse(typeof(DayOfWeek), nextRelease.First())) == DateTime.Now.DayOfWeek)
            || !nextRelease.Any()) {

            int latestRelease;
            var domainNameOfUrl = new Uri(url).Host;

            switch (domainNameOfUrl) {
              case "www.mangakakalot.com":
                latestRelease = GetLatestReleaseFromMangakakalot(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "mangakakalot.com":
                latestRelease = GetLatestReleaseFromMangakakalot(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "www.pahe.win":
                latestRelease = GetLatestReleaseFromPahe(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "pahe.win":
                latestRelease = GetLatestReleaseFromPahe(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "www.mangahub.io":
                latestRelease = GetLatestReleaseFromMangaHub(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "mangahub.io":
                latestRelease = GetLatestReleaseFromMangaHub(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "www.toomics.com":
                latestRelease = GetLatestReleaseFromToomics(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "toomics.com":
                latestRelease = GetLatestReleaseFromToomics(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "www.readmanganato.com":
                latestRelease = GetLatestReleaseFromManganato(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
                break;
              case "readmanganato.com":
                latestRelease = GetLatestReleaseFromManganato(url);
                await _trackerRepository.BulkUpdateTracker(url, latestRelease);
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
