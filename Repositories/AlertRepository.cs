using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MangaAlert.Entities;
using MangaAlert.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MangaAlert.Repositories
{
  public class AlertRepository: IAlertRepository
  {
    private const string CollectionName = "alerts";
    private readonly IMongoCollection<Alert> _alertsCollection;
    private readonly FilterDefinitionBuilder<Alert> _filterBuilder = Builders<Alert>.Filter;
    private readonly UpdateDefinitionBuilder<Alert> _updateBuilder = Builders<Alert>.Update;

    public AlertRepository(IMongoDbSettings settings)
    {
      var client = new MongoClient(settings.ConnectionString);
      IMongoDatabase database = client.GetDatabase(settings.DatabaseName);
      _alertsCollection = database.GetCollection<Alert>(CollectionName);
    }

    public async Task<Alert> GetAlertForUser(Guid alertId, Guid userId)
    {
      var filter =
        _filterBuilder.Eq(alert => alert.Id, alertId)
        & _filterBuilder.Eq(alert => alert.UserId, userId);

      return await _alertsCollection.Find(filter).SingleOrDefaultAsync();
    }

    #nullable enable
    public async Task<IEnumerable<Alert>> GetAlertsForUser(
      Guid userId,
      string type,
      string? status,
      string? sort,
      int? limit,
      int? offset,
      bool hasCompleted
      )
    {
      var filter = _filterBuilder.Eq(alert => alert.UserId, userId)
        & _filterBuilder.Eq(alert => alert.Type, type.FirstCharToUpper());
      var sortObject = new BsonDocument("LatestChapterUpdatedAt", -1);

      if (!string.IsNullOrEmpty(status)) {
        filter = _filterBuilder.Eq(alert => alert.UserId, userId)
          & _filterBuilder.Eq(alert => alert.Status, status.FirstCharToUpper())
          & _filterBuilder.Eq(alert => alert.Type, type.FirstCharToUpper());
      }

      if (!string.IsNullOrEmpty(sort) && sort == "Title") {
        sortObject = new BsonDocument(sort.FirstCharToUpper(), -1);
      }

      if (hasCompleted) {
        filter = _filterBuilder.Eq(alert => alert.UserId, userId)
                 & _filterBuilder.Eq(alert => alert.Type, type.FirstCharToUpper())
                 & _filterBuilder.Eq(alert => alert.HasCompleted, true);
      }

      return await _alertsCollection
        .Find(filter)
        .Skip((offset - 1) * limit)
        .Sort(sortObject)
        .Limit(limit)
        .ToListAsync();
    }

    public async Task CreateAlert(Alert alert)
    {
      await _alertsCollection.InsertOneAsync(alert);
    }

    public async Task UpdateAlert(Alert alert)
    {
      var filter = _filterBuilder.Eq(existingAlert => existingAlert.Id, alert.Id);

      await _alertsCollection.ReplaceOneAsync(filter, alert);
     }

    public async Task DeleteAlert(Guid alertId)
    {
      var filter = _filterBuilder.Eq(alert => alert.Id, alertId);

      await _alertsCollection.DeleteOneAsync(filter);
    }

    public async Task<List<string>> GetAllUniqueAlertsByUrl()
    {
      return await _alertsCollection.Distinct<string>("Url", new BsonDocument()).ToListAsync();
    }

    public async Task<List<string>> GetNextReleaseForUrl(string url)
    {
      var filter = _filterBuilder.Eq(alert => alert.Url, url);
      return await _alertsCollection.Distinct<string>("NextRelease", filter).ToListAsync();
    }

    public async Task BulkUpdateAlert(string url, int latestRelease)
    {
      var filter = _filterBuilder.Eq(alert => alert.Url, url);
      var update = _updateBuilder.Set(alert => alert.LatestRelease, latestRelease)
        .Set(alert => alert.LatestReleaseUpdatedAt, DateTimeOffset.Now);

      await _alertsCollection.UpdateManyAsync(filter, update);

      // var allAlertsWithSameUrl = await _alertsCollection.Find(filter).ToListAsync();
      update = _updateBuilder.Set(alert => alert.HasSeenLatestRelease, false);
      filter = _filterBuilder.Eq(alert => alert.Url, url)
               & _filterBuilder.Eq(alert => alert.LatestRelease, latestRelease - 1);

      await _alertsCollection.UpdateManyAsync(filter, update);

      // foreach (var alert in allAlertsWithSameUrl.Where(alert => alert.LatestRelease == latestRelease - 1)) {
      //   await _alertsCollection.UpdateOneAsync(_filterBuilder.Eq(alert => alert.Id, alert.Id), update);
      // }
    }

    public async Task ToggleReleaseSeen(Guid alertId, bool seen)
    {
      var filter = _filterBuilder.Eq(alert => alert.Id, alertId);
      var update = _updateBuilder.Set(alert => alert.HasSeenLatestRelease, seen);

      await _alertsCollection.UpdateOneAsync(filter, update);
    }

    public async Task<long> GetAlertsCountForUser(Guid userId, string type, string? status)
    {
      var filter = _filterBuilder.Eq(alert => alert.UserId, userId)
                   & _filterBuilder.Eq(alert => alert.Type, type.FirstCharToUpper());

      if (!string.IsNullOrWhiteSpace(status)) {
        filter = _filterBuilder.Eq(alert => alert.UserId, userId)
                 & _filterBuilder.Eq(alert => alert.Status, status.FirstCharToUpper())
                 & _filterBuilder.Eq(alert => alert.Type, type.FirstCharToUpper());
      }

      return await _alertsCollection.CountDocumentsAsync(filter);
    }

    public async Task ToggleComplete(Guid alertId, bool completed)
    {
      var filter = _filterBuilder.Eq(alert => alert.Id, alertId);
      var update = _updateBuilder.Set(alert => alert.HasCompleted, completed)
        .Set(alert => alert.CompletedAt, DateTimeOffset.Now);

      await _alertsCollection.UpdateOneAsync(filter, update);
    }
  }
}
