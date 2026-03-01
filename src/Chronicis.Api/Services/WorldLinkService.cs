using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class WorldLinkService : IWorldLinkService
{
    private readonly ChronicisDbContext _context;

    public WorldLinkService(ChronicisDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<List<WorldLinkDto>>> GetWorldLinksAsync(Guid worldId, Guid userId)
    {
        var world = await _context.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == userId);

        if (world == null)
        {
            return ServiceResult<List<WorldLinkDto>>.NotFound("World not found or access denied");
        }

        var links = await _context.WorldLinks
            .AsNoTracking()
            .Where(wl => wl.WorldId == worldId)
            .OrderBy(wl => wl.Title)
            .Select(wl => new WorldLinkDto
            {
                Id = wl.Id,
                WorldId = wl.WorldId,
                Url = wl.Url,
                Title = wl.Title,
                Description = wl.Description,
                CreatedAt = wl.CreatedAt
            })
            .ToListAsync();

        return ServiceResult<List<WorldLinkDto>>.Success(links);
    }

    public async Task<ServiceResult<WorldLinkDto>> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto, Guid userId)
    {
        var world = await _context.Worlds
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == userId);

        if (world == null)
        {
            return ServiceResult<WorldLinkDto>.NotFound("World not found or access denied");
        }

        var link = new WorldLink
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Url = dto.Url.Trim(),
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.WorldLinks.Add(link);
        await _context.SaveChangesAsync();

        return ServiceResult<WorldLinkDto>.Success(new WorldLinkDto
        {
            Id = link.Id,
            WorldId = link.WorldId,
            Url = link.Url,
            Title = link.Title,
            Description = link.Description,
            CreatedAt = link.CreatedAt
        });
    }

    public async Task<ServiceResult<WorldLinkDto>> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto, Guid userId)
    {
        var world = await _context.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == userId);

        if (world == null)
        {
            return ServiceResult<WorldLinkDto>.NotFound("World not found or access denied");
        }

        var link = await _context.WorldLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorldId == worldId);

        if (link == null)
        {
            return ServiceResult<WorldLinkDto>.NotFound("Link not found");
        }

        link.Url = dto.Url.Trim();
        link.Title = dto.Title.Trim();
        link.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await _context.SaveChangesAsync();

        return ServiceResult<WorldLinkDto>.Success(new WorldLinkDto
        {
            Id = link.Id,
            WorldId = link.WorldId,
            Url = link.Url,
            Title = link.Title,
            Description = link.Description,
            CreatedAt = link.CreatedAt
        });
    }

    public async Task<ServiceResult<bool>> DeleteWorldLinkAsync(Guid worldId, Guid linkId, Guid userId)
    {
        var world = await _context.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == userId);

        if (world == null)
        {
            return ServiceResult<bool>.NotFound("World not found or access denied");
        }

        var link = await _context.WorldLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorldId == worldId);

        if (link == null)
        {
            return ServiceResult<bool>.NotFound("Link not found");
        }

        _context.WorldLinks.Remove(link);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }
}

