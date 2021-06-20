using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MangaAlert.Entities;
using MangaAlert.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MangaAlert.Repositories
{
  public class TrackerRepository: ITrackerRepository
  {
    private const string CollectionName = "trackers";
    private readonly IMongoCollection<Tracker> _trackersCollection;
    private readonly FilterDefinitionBuilder<Tracker> _filterBuilder = Builders<Tracker>.Filter;
    private readonly UpdateDefinitionBuilder<Tracker> _updateBuilder = Builders<Tracker>.Update;

    public TrackerRepository(IMongoDbSettings settings)
    {
      var client = new MongoClient(settings.ConnectionString);
      IMongoDatabase database = client.GetDatabase(settings.DatabaseName);
      _trackersCollection = database.GetCollection<Tracker>(CollectionName);
    }

    public async Task<Tracker> GetTrackerForUser(Guid trackerId, Guid userId)
    {
      var filter =
        _filterBuilder.Eq(tracker => tracker.Id, trackerId)
        & _filterBuilder.Eq(tracker => tracker.UserId, userId);

      return await _trackersCollection.Find(filter).SingleOrDefaultAsync();
    }

    #nullable enable
    public async Task<IEnumerable<Tracker>> GetTrackersForUser(Guid userId,
      string? type,
      string? status,
      string? sort,
      int? limit,
      int? offset,
      bool hasCompleted,
      string? search)
    {
      if (!string.IsNullOrWhiteSpace(search)) {
        var queryExpr = new BsonRegularExpression(new Regex(search.FirstCharToUpper(), RegexOptions.None));

        var textFilter = _filterBuilder.Eq(tracker => tracker.UserId, userId)
                          & _filterBuilder.Text(search);

        var regexFilter = _filterBuilder.Eq(tracker => tracker.UserId, userId)
                          & _filterBuilder.Regex("Title", queryExpr);

        var trackers = await Task.WhenAll(
          _trackersCollection
            .Find(textFilter)
            .Skip((offset - 1) * limit)
            .Sort(new BsonDocument("Title", 1))
            .Limit(limit)
            .ToListAsync(),
          _trackersCollection
            .Find(regexFilter)
            .Skip((offset - 1) * limit)
            .Sort(new BsonDocument("Title", 1))
            .Limit(limit)
            .ToListAsync());

        return trackers.First().Concat(trackers.ElementAt(1));
      }

      FilterDefinition<Tracker> filter;

      if (type is "Manga" or "Anime") {
        filter = _filterBuilder.Eq(tracker => tracker.UserId, userId)
                 & _filterBuilder.Eq(tracker => tracker.Type, type.FirstCharToUpper());
      } else {
        filter = _filterBuilder.Eq(tracker => tracker.UserId, userId);
      }

      var sortObject = new BsonDocument("LatestChapterUpdatedAt", -1);

      if (!string.IsNullOrWhiteSpace(status)) {
        Definitions.StatusTypes result;
        if (Enum.TryParse(status, out  result) && result == Definitions.StatusTypes.PlanToReadAndWatch) {
          filter &= _filterBuilder.Or(_filterBuilder.Eq(tracker => tracker.Status, "PlanToWatch"),
            _filterBuilder.Eq(tracker => tracker.Status, "PlanToRead"));
        } else if (Enum.TryParse(status, out  result) && result == Definitions.StatusTypes.ReadingAndWatching) {
          filter &= _filterBuilder.Or(_filterBuilder.Eq(tracker => tracker.Status, "Reading"),
            _filterBuilder.Eq(tracker => tracker.Status, "Watching"));
        } else {
          filter &= _filterBuilder.Eq(tracker => tracker.Type, type.FirstCharToUpper())
                    & _filterBuilder.Eq(tracker => tracker.Status, status.FirstCharToUpper());
        }
      }

      if (!string.IsNullOrWhiteSpace(sort) && sort == "Title") {
        sortObject = new BsonDocument(sort.FirstCharToUpper(), 1);
      }

      if (hasCompleted) {
        filter &= _filterBuilder.Eq(tracker => tracker.HasCompleted, true);
      }

      return (await _trackersCollection
        .Find(filter)
        .Skip((offset - 1) * limit)
        .Sort(sortObject)
        .Limit(limit)
        .ToListAsync());
    }

    public async Task CreateTracker(Tracker tracker)
    {
      await _trackersCollection.InsertOneAsync(tracker);
    }

    public async Task UpdateTracker(Tracker tracker)
    {
      var filter = _filterBuilder.Eq(existingTracker => existingTracker.Id, tracker.Id);

      await _trackersCollection.ReplaceOneAsync(filter, tracker);
     }

    public async Task DeleteTracker(Guid trackerId)
    {
      var filter = _filterBuilder.Eq(tracker => tracker.Id, trackerId);

      await _trackersCollection.DeleteOneAsync(filter);
    }

    public async Task<List<string>> GetAllUniqueTrackersByUrl()
    {
      return await _trackersCollection.Distinct<string>("Url", new BsonDocument()).ToListAsync();
    }

    public async Task<List<string>> GetReleaseScheduleForUrl(string url)
    {
      var filter = _filterBuilder.Eq(tracker => tracker.Url, url);
      return await _trackersCollection.Distinct<string>("ReleasesOn", filter).ToListAsync();
    }

    public async Task BulkUpdateTracker(string url, int latestRelease)
    {
      var filter = _filterBuilder.Eq(tracker => tracker.Url, url);
      var update = _updateBuilder.Set(tracker => tracker.LatestRelease, latestRelease)
        .Set(tracker => tracker.LatestReleaseUpdatedAt, DateTimeOffset.Now);

      await _trackersCollection.UpdateManyAsync(filter, update);

      // var allAlertsWithSameUrl = await _trackersCollection.Find(filter).ToListAsync();
      update = _updateBuilder.Set(tracker => tracker.HasSeenLatestRelease, false);
      filter = _filterBuilder.Eq(tracker => tracker.Url, url)
               & _filterBuilder.Eq(tracker => tracker.LatestRelease, latestRelease - 1);

      Console.WriteLine(latestRelease);

      await _trackersCollection.UpdateManyAsync(filter, update);

      // foreach (var tracker in allAlertsWithSameUrl.Where(tracker => tracker.LatestRelease == latestRelease - 1)) {
      //   await _trackersCollection.UpdateOneAsync(_filterBuilder.Eq(tracker => tracker.Id, tracker.Id), update);
      // }
    }

    public async Task ToggleReleaseSeen(Guid trackerId, bool seen)
    {
      var filter = _filterBuilder.Eq(tracker => tracker.Id, trackerId);
      var update = _updateBuilder.Set(tracker => tracker.HasSeenLatestRelease, seen);

      await _trackersCollection.UpdateOneAsync(filter, update);
    }

    public async Task<long> GetTrackersCountForUser(
      Guid userId,
      string? type,
      string? status,
      string? search,
      bool hasCompleted
      )
    {
      if (!string.IsNullOrWhiteSpace(search)) {
        var queryExpr = new BsonRegularExpression(new Regex(search.FirstCharToUpper(), RegexOptions.None));

        var textFilter = _filterBuilder.Eq(tracker => tracker.UserId, userId)
                         & _filterBuilder.Text(search);

        var regexFilter = _filterBuilder.Eq(tracker => tracker.UserId, userId)
                          & _filterBuilder.Regex("Title", queryExpr);

        var trackers = await Task.WhenAll(
          _trackersCollection
            .Find(textFilter)
            .ToListAsync(),
          _trackersCollection
            .Find(regexFilter)
            .ToListAsync());

        return trackers.First().Concat(trackers.ElementAt(1)).Distinct().LongCount();
      }

      FilterDefinition<Tracker> filter;

      if (type is "Manga" or "Anime") {
        filter = _filterBuilder.Eq(tracker => tracker.UserId, userId)
                 & _filterBuilder.Eq(tracker => tracker.Type, type.FirstCharToUpper());
      } else {
        filter = _filterBuilder.Eq(tracker => tracker.UserId, userId);
      }

      if (!string.IsNullOrWhiteSpace(status)) {
        Definitions.StatusTypes result;
        if (Enum.TryParse(status, out  result) && result == Definitions.StatusTypes.PlanToReadAndWatch) {
          filter &= _filterBuilder.Or(_filterBuilder.Eq(tracker => tracker.Status, "PlanToWatch"),
            _filterBuilder.Eq(tracker => tracker.Status, "PlanToRead"));
        } else if (Enum.TryParse(status, out  result) && result == Definitions.StatusTypes.ReadingAndWatching) {
          filter &= _filterBuilder.Or(_filterBuilder.Eq(tracker => tracker.Status, "Reading"),
            _filterBuilder.Eq(tracker => tracker.Status, "Watching"));
        } else {
          filter &= _filterBuilder.Eq(tracker => tracker.Type, type.FirstCharToUpper())
                    & _filterBuilder.Eq(tracker => tracker.Status, status.FirstCharToUpper());
        }
      }

      if (hasCompleted) {
        filter &= _filterBuilder.Eq(tracker => tracker.HasCompleted, true);
      }

      return await _trackersCollection.CountDocumentsAsync(filter);
    }

    public async Task ToggleComplete(Guid trackerId, bool completed)
    {
      var filter = _filterBuilder.Eq(tracker => tracker.Id, trackerId);
      var update = _updateBuilder.Set(tracker => tracker.HasCompleted, completed)
        .Set(tracker => tracker.CompletedAt, DateTimeOffset.Now);

      await _trackersCollection.UpdateOneAsync(filter, update);
    }
  }
}
