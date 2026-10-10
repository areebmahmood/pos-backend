using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class StoreSettingsRepository(PosDbContext db) : IStoreSettingsRepository
{
    public async Task<StoreSettings> GetAsync(CancellationToken ct)
    {
        var settings = await db.StoreSettings.FirstOrDefaultAsync(s => s.Id == StoreSettings.SingletonId, ct);
        if (settings is not null)
            return settings;

        settings = StoreSettings.CreateDefault();
        db.StoreSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }
}
