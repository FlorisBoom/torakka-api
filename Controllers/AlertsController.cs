using System;
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
  public class AlertsController: ControllerBase
  {
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;

    private string[] _supportedWebsites = {
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

    private bool IsValidStatus(string alertType, string status)
    {
      var type = alertType.FirstCharToUpper();

      // ReSharper disable once HeapView.BoxingAllocation
      if (type == Definitions.AlertTypes.Anime.ToString()) {
        if (!Enum.IsDefined(typeof(Definitions.AnimeStatusTypes), status.FirstCharToUpper())) {
          return false;
        }
        // ReSharper disable once HeapView.BoxingAllocation
      } else if (type == Definitions.AlertTypes.Manga.ToString()) {
        if (!Enum.IsDefined(typeof(Definitions.MangaStatusTypes), status.FirstCharToUpper())) {
          return false;
        }
      }

      return true;
    }

    public AlertsController(IAlertRepository alertRepository, IUserRepository userRepository)
    {
      this._alertRepository = alertRepository;
      this._userRepository = userRepository;
    }

    // GET /alerts/{userId}/{alertId}
    [HttpGet("{userId}/{alertId}")]
    public async Task<ActionResult<AlertDto>> GetAlertForUser(Guid userId, Guid alertId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to access this alert."
        });
      }

      Alert alert = await _alertRepository.GetAlertForUser(alertId, userId);

      if (alert is null) return NotFound();

      return Ok(new {
        data = alert.AsDto()
      });
    }

    // GET /alerts/{userId}
    [HttpGet("{userId}")]
    public async Task<ActionResult<AlertDto>> GetAlertsForUser(Guid userId, QueryOptionsDto queryOptionsDto)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the access to this users alerts."
        });
      }

      if (!string.IsNullOrWhiteSpace(queryOptionsDto.Status) && !IsValidStatus(queryOptionsDto.Type, queryOptionsDto.Status)) {
        return StatusCode(422, new {
          message = $"Status {queryOptionsDto.Status} not allowed."
        });
      }

      if (!string.IsNullOrWhiteSpace(queryOptionsDto.SortBy)) {
        if (!Enum.IsDefined(typeof(Definitions.SortTypes), queryOptionsDto.SortBy.FirstCharToUpper())) {
          return StatusCode(422, new {
            message = $"Sort by {queryOptionsDto.SortBy} not allowed."
          });
        }
      }

      if (!string.IsNullOrWhiteSpace(queryOptionsDto.SortOption)) {
        if (!Enum.IsDefined(typeof(Definitions.SortOptions), queryOptionsDto.SortOption.FirstCharToUpper())) {
          return StatusCode(422, new {
            message = $"Sort option {queryOptionsDto.SortOption} not allowed."
          });
        }
      }

      int? sortOption = null;
      if (queryOptionsDto.SortOption != null) {
        sortOption = (int)Enum.Parse(typeof(Definitions.SortOptions), queryOptionsDto.SortOption.FirstCharToUpper());
      }

      var alerts = (await _alertRepository.GetAlertsForUser(
          userId,
          queryOptionsDto.Type.FirstCharToUpper(),
          queryOptionsDto.Status,
          queryOptionsDto.Limit,
          queryOptionsDto.SortBy,
          sortOption,
          queryOptionsDto.Page
          ))
        .Select(alert => alert.AsDto());

      return Ok(new {
        data = alerts
      });
    }

    // POST /alerts/{userId}
    [HttpPost("{userId}")]
    public async Task<ActionResult<AlertDto>> CreateAlert(Guid userId, CreateAlertDto alertDto)
    {
      var user = await _userRepository.GetUser(userId);

      if (user is null) {
        return NotFound(new {
          message = "User not found"
        });
      }

      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to create alert."
        });
      }

      if (!Enum.IsDefined(typeof(Definitions.AlertTypes), alertDto.Type.FirstCharToUpper())) {
        return StatusCode(422, new {
          message = $"Type {alertDto.Type} not allowed."
        });
      }

      if (!IsValidStatus(alertDto.Type, alertDto.Status)) {
        return StatusCode(422, new {
          message = $"Status {alertDto.Status} not allowed."
        });
      }

      if (!Array.Exists(_supportedWebsites, site => site == new Uri(alertDto.Url).Host)) {
        return StatusCode(422, new {
          message = $"Url {alertDto.Url} not allowed."
        });
      }

      if (!string.IsNullOrWhiteSpace(alertDto.ReleasesOn)
          && !Enum.TryParse<DayOfWeek>(alertDto.ReleasesOn.FirstCharToUpper(), out DayOfWeek day)
        && day == DateTime.Today.DayOfWeek) {
        return StatusCode(422, new {
          message = $"Releases on {alertDto.ReleasesOn} not allowed."
        });
      }

      Alert alert = new() {
        Id = Guid.NewGuid(),
        Type = alertDto.Type.FirstCharToUpper(),
        Title = alertDto.Title,
        Url = alertDto.Url,
        UserReleaseProgress = alertDto.UserReleaseProgress,
        LatestRelease = alertDto.LatestRelease,
        Status = alertDto.Status.FirstCharToUpper(),
        UserId = userId,
        CreatedAt = DateTimeOffset.Now,
        ReleasesOn = !string.IsNullOrWhiteSpace(alertDto.ReleasesOn) ? alertDto.ReleasesOn : null
      };

      await _alertRepository.CreateAlert(alert);

      var returnObject = new {
        data = alert.AsDto()
      };

      return CreatedAtAction(nameof(GetAlertForUser), new {
        userId = alert.UserId, alertId = alert.Id
      }, returnObject);
    }

    // PUT /alerts/{userId}/{alertId}
    [HttpPut("{userId}/{alertId}")]
    public async Task<ActionResult<AlertDto>> UpdateAlert(Guid userId, Guid alertId, UpdateAlertDto alertDto)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to update this alert."
        });
      }

      if (!IsValidStatus(alertDto.Type, alertDto.Status)) {
        return StatusCode(422, new {
          message = "Status not allowed."
        });
      }

      if (!Enum.TryParse<DayOfWeek>(alertDto.ReleasesOn.FirstCharToUpper(), out DayOfWeek day)
          && day == DateTime.Today.DayOfWeek) {
        return StatusCode(422, new {
          message = $"NextRelease {alertDto.ReleasesOn} not allowed."
        });
      }

      Alert existingAlert = await _alertRepository.GetAlertForUser(alertId, userId);

      if (existingAlert is null) return NotFound();

      Alert updatedAlert = (existingAlert with {
        Type = alertDto.Type,
        Title = alertDto.Title,
        Url = alertDto.Url,
        UserReleaseProgress = alertDto.UserReleaseProgress,
        LatestRelease = alertDto.LatestRelease,
        Status = alertDto.Status.FirstCharToUpper(),
        ReleasesOn = alertDto.ReleasesOn.FirstCharToUpper()
      });

      await _alertRepository.UpdateAlert(updatedAlert);

      return Ok(new {
        data = updatedAlert.AsDto()
      });
    }

    // DELETE /alerts/{userId}/{alertId}
    [HttpDelete("{userId}/{alertId}")]
    public async Task<ActionResult> DeleteAlert(Guid userId, Guid alertId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to delete this alert."
        });
      }

      Alert existingAlert = await _alertRepository.GetAlertForUser(alertId, userId);

      if (existingAlert is null) return NotFound();

      await _alertRepository.DeleteAlert(alertId);

      return StatusCode(200,  new {
        data = "success"
      });
    }

    // Post /alerts/{userId}/{alertId}/seen
    [HttpPost("{userId}/{alertId}/seen")]
    public async Task<ActionResult> ToggleLatestReleaseSeen(Guid userId, Guid alertId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to delete this alert."
        });
      }

      Alert existingAlert = await _alertRepository.GetAlertForUser(alertId, userId);

      if (existingAlert is null) return NotFound();

      await _alertRepository.ToggleReleaseSeen(alertId, !existingAlert.HasSeenLatestRelease);

      return StatusCode(200,  new {
        data = "success"
      });
    }
  }
}
