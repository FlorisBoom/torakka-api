using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Entities;
using MangaAlert.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaAlert.Controllers
{
  [ApiController]
  [Authorize]
  [Produces("application/json")]
  [Route("[controller]")]
  public class TrackersController: ControllerBase
  {
    private readonly ITrackerRepository _trackerRepository;
    private readonly IUserRepository _userRepository;

    private readonly string[] _supportedWebsites = {
      "www.mangakakalot.com",
      "mangakakalot.com",
      "www.pahe.win",
      "pahe.win",
      "www.mangahub.io",
      "mangahub.io",
      "www.toomics.com",
      "toomics.com",
      "www.readmanganato.com",
      "readmanganato.com"
    };

    private bool IsValidStatus(string trackerType, string status)
    {
      var type = trackerType.FirstCharToUpper();

      Definitions.TrackerTypes result;
      if (Enum.TryParse(type, out  result) && result == Definitions.TrackerTypes.Anime) {
        if (!Enum.IsDefined(typeof(Definitions.AnimeStatusTypes), status.FirstCharToUpper())) {
          return false;
        }
      } else if (Enum.TryParse(type, out  result) && result == Definitions.TrackerTypes.Manga) {
        if (!Enum.IsDefined(typeof(Definitions.MangaStatusTypes), status.FirstCharToUpper())) {
          return false;
        }
      } else if (Enum.TryParse(type, out  result) && result == Definitions.TrackerTypes.All) {
        if (!Enum.IsDefined(typeof(Definitions.StatusTypes), status.FirstCharToUpper())) {
          return false;
        }
      }

      return true;
    }

    public TrackersController(ITrackerRepository trackerRepository, IUserRepository userRepository)
    {
      this._trackerRepository = trackerRepository;
      this._userRepository = userRepository;
    }

    // GET /trackers/{userId}/{trackerId}
    [HttpGet("{userId}/{trackerId}")]
    public async Task<ActionResult<TrackerDto>> GetTrackerForUser(Guid userId, Guid trackerId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to access this tracker."
        });
      }

      Tracker tracker = await _trackerRepository.GetTrackerForUser(trackerId, userId);

      if (tracker is null) return NotFound();

      return Ok(new {
        data = tracker.AsDto()
      });
    }

    // GET /trackers/{userId}
    [HttpGet("{userId}")]
    public async Task<ActionResult<TrackerDto>> GetTrackersForUser(
      Guid userId,
      string type,
      string status = null,
      string sort = null,
      int limit = 20,
      int offset = 1,
      string search = null
      )
    {
      IEnumerable<TrackerDto> trackers;
      long count;

      if (!string.IsNullOrWhiteSpace(search)) {
        trackers = (await _trackerRepository.GetTrackersForUser(
          userId,
          null,
          null,
          null,
          limit,
          offset,
          false,
          search
        )).Select(tracker => tracker.AsDto()).Distinct();

        count = await _trackerRepository.GetTrackersCountForUser(
          userId,
          null,
          null,
          search,
          false
          );

        return Ok(new {
          data = trackers,
          limit,
          offset,
          totalCount = count,
        });
      }

      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the access to this users trackers."
        });
      }

      if (string.IsNullOrWhiteSpace(type)) {
        return StatusCode(422, new {
          message = "Type cannot be empty"
        });
      }

      if (!Enum.IsDefined(typeof(Definitions.TrackerTypes), type.FirstCharToUpper())) {
        return StatusCode(422, new {
          message = $"Type {type} not allowed."
        });
      }

      if (!string.IsNullOrWhiteSpace(status) && !IsValidStatus(type, status)) {
        return StatusCode(422, new {
          message = $"Status {status} not allowed."
        });
      }

      if (!string.IsNullOrWhiteSpace(sort)) {
        if (!Enum.IsDefined(typeof(Definitions.SortTypes), sort.FirstCharToUpper())) {
          return StatusCode(422, new {
            message = $"Sort by {sort} not allowed."
          });
        }
      }

      var hasCompleted = false;
      if (!string.IsNullOrWhiteSpace(status)) {
        if (status == "Completed") {
          status = null;
          hasCompleted = true;
        }
      }

      trackers = (await _trackerRepository.GetTrackersForUser(
        userId,
        type,
        status,
        sort,
        limit,
        offset,
        hasCompleted,
        null
      )).Select(tracker => tracker.AsDto());

      count = await _trackerRepository.GetTrackersCountForUser(
        userId,
        type,
        status,
        null,
        hasCompleted);

      return Ok(new {
        data = trackers,
        limit,
        offset,
        totalCount = count,
      });
    }

    // POST /trackers/{userId}
    [HttpPost("{userId}")]
    public async Task<ActionResult<TrackerDto>> CreateTracker(Guid userId, CreateTrackerDto trackerDto)
    {
      var user = await _userRepository.GetUser(userId);

      if (user is null) {
        return NotFound(new {
          message = "User not found"
        });
      }

      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to create tracker."
        });
      }

      if (!Enum.IsDefined(typeof(Definitions.TrackerTypes), trackerDto.Type.FirstCharToUpper())) {
        return StatusCode(422, new {
          message = $"Type {trackerDto.Type} not allowed."
        });
      }

      if (!IsValidStatus(trackerDto.Type, trackerDto.Status)) {
        return StatusCode(422, new {
          message = $"Status {trackerDto.Status} not allowed."
        });
      }

      if (!Array.Exists(_supportedWebsites, site => site == new Uri(trackerDto.Url).Host)) {
        return StatusCode(422, new {
          message = $"Url {trackerDto.Url} not allowed."
        });
      }

      if (!string.IsNullOrWhiteSpace(trackerDto.ReleasesOn)
          && !Enum.TryParse<DayOfWeek>(trackerDto.ReleasesOn.FirstCharToUpper(), out DayOfWeek day)
        && day == DateTime.Today.DayOfWeek) {
        return StatusCode(422, new {
          message = $"Releases on {trackerDto.ReleasesOn} not allowed."
        });
      }

      Tracker tracker = new() {
        Id = Guid.NewGuid(),
        Type = trackerDto.Type.FirstCharToUpper(),
        Title = trackerDto.Title,
        Url = trackerDto.Url,
        UserReleaseProgress = trackerDto.UserReleaseProgress,
        LatestRelease = trackerDto.LatestRelease,
        Status = trackerDto.Status.FirstCharToUpper(),
        UserId = userId,
        CreatedAt = DateTimeOffset.Now,
        ReleasesOn = !string.IsNullOrWhiteSpace(trackerDto.ReleasesOn) ? trackerDto.ReleasesOn.FirstCharToUpper() : null,
        ImageUrl = trackerDto.ImageUrl
      };

      await _trackerRepository.CreateTracker(tracker);

      var returnObject = new {
        data = tracker.AsDto()
      };

      return CreatedAtAction(nameof(GetTrackerForUser), new {
        userId = tracker.UserId, trackerId = tracker.Id
      }, returnObject);
    }

    // PUT /trackers/{userId}/{trackerId}
    [HttpPut("{userId}/{trackerId}")]
    public async Task<ActionResult<TrackerDto>> UpdateTracker(Guid userId, Guid trackerId, UpdateTrackerDto trackerDto)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to update this tracker."
        });
      }

      if (!IsValidStatus(trackerDto.Type, trackerDto.Status)) {
        return StatusCode(422, new {
          message = "Status not allowed."
        });
      }

      if (!string.IsNullOrWhiteSpace(trackerDto.ReleasesOn)) {
        if (!Enum.TryParse<DayOfWeek>(trackerDto.ReleasesOn.FirstCharToUpper(), out DayOfWeek day)
            && day == DateTime.Today.DayOfWeek) {
          return StatusCode(422, new {
            message = $"NextRelease {trackerDto.ReleasesOn} not allowed."
          });
        }
      }

      if (!Array.Exists(_supportedWebsites, site => site == new Uri(trackerDto.Url).Host)) {
        return StatusCode(422, new {
          message = $"Url {trackerDto.Url} not allowed."
        });
      }

      Tracker existingTracker = await _trackerRepository.GetTrackerForUser(trackerId, userId);

      if (existingTracker is null) return NotFound();

      Tracker updatedTracker = (existingTracker with {
        Type = trackerDto.Type,
        Title = trackerDto.Title,
        Url = trackerDto.Url,
        UserReleaseProgress = trackerDto.UserReleaseProgress,
        LatestRelease = trackerDto.LatestRelease,
        Status = trackerDto.Status.FirstCharToUpper(),
        ReleasesOn = !string.IsNullOrWhiteSpace(trackerDto.ReleasesOn) ? trackerDto.ReleasesOn.FirstCharToUpper() : null,
        ImageUrl = trackerDto.ImageUrl
      });

      await _trackerRepository.UpdateTracker(updatedTracker);

      return Ok(new {
        data = updatedTracker.AsDto()
      });
    }

    // DELETE /trackers/{userId}/{trackerId}
    [HttpDelete("{userId}/{trackerId}")]
    public async Task<ActionResult> DeleteTracker(Guid userId, Guid trackerId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to delete this tracker."
        });
      }

      Tracker existingTracker = await _trackerRepository.GetTrackerForUser(trackerId, userId);

      if (existingTracker is null) return NotFound();

      await _trackerRepository.DeleteTracker(trackerId);

      return StatusCode(200,  new {
        data = "success"
      });
    }

    // Post /trackers/{userId}/{trackerId}/seen
    [HttpPost("{userId}/{trackerId}/seen")]
    public async Task<ActionResult> ToggleLatestReleaseSeen(Guid userId, Guid trackerId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to this tracker."
        });
      }

      Tracker existingTracker = await _trackerRepository.GetTrackerForUser(trackerId, userId);

      if (existingTracker is null) return NotFound();

      await _trackerRepository.ToggleReleaseSeen(trackerId, !existingTracker.HasSeenLatestRelease);

      return StatusCode(200,  new {
        data = "success"
      });
    }

    // Post /trackers/{userId}/{trackerId}/complete
    [HttpPost("{userId}/{trackerId}/complete")]
    public async Task<ActionResult> ToggleComplete(Guid userId, Guid trackerId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to this tracker."
        });
      }

      Tracker existingTracker = await _trackerRepository.GetTrackerForUser(trackerId, userId);

      if (existingTracker is null) return NotFound();

      await _trackerRepository.ToggleComplete(trackerId, !existingTracker.HasCompleted);

      return StatusCode(200,  new {
        data = "success"
      });
    }
  }
}
