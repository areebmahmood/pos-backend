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

    public Task<StoreImage?> GetImageAsync(string kind, CancellationToken ct) =>
        db.StoreImages.FirstOrDefaultAsync(i => i.Kind == kind, ct);

    public Task<DateTime?> GetImageUpdatedAtAsync(string kind, CancellationToken ct) =>
        db.StoreImages.AsNoTracking().Where(i => i.Kind == kind).Select(i => (DateTime?)i.UpdatedAt).FirstOrDefaultAsync(ct);

    public void AddImage(StoreImage image) => db.StoreImages.Add(image);

    public void RemoveImage(StoreImage image) => db.StoreImages.Remove(image);
}
