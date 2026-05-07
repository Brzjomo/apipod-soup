using APIPodSoup.Core.Data;
using APIPodSoup.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace APIPodSoup.Core.Services;

public class HistoryService : IHistoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public HistoryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<HistoryRecord>> GetAllAsync(int skip = 0, int take = 20)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.HistoryRecords
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<HistoryRecord?> GetByIdAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.HistoryRecords.FindAsync(id);
    }

    public async Task<HistoryRecord> CreateAsync(HistoryRecord record)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.HistoryRecords.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task UpdateAsync(HistoryRecord record)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.HistoryRecords.Update(record);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var record = await db.HistoryRecords.FindAsync(id);
        if (record != null)
        {
            // Delete associated result blobs first
            var blobs = await db.ResultBlobs.Where(b => b.HistoryRecordId == id).ToListAsync();
            db.ResultBlobs.RemoveRange(blobs);
            db.HistoryRecords.Remove(record);
            await db.SaveChangesAsync();

            // Reclaim disk space
            await db.Database.ExecuteSqlRawAsync("VACUUM");
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.HistoryRecords.CountAsync();
    }

    public async Task SaveResultBlobsAsync(string historyRecordId, List<ResultBlob> blobs)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        // Remove old blobs for this record (replace, don't accumulate)
        var old = await db.ResultBlobs.Where(b => b.HistoryRecordId == historyRecordId).ToListAsync();
        db.ResultBlobs.RemoveRange(old);
        db.ResultBlobs.AddRange(blobs);
        await db.SaveChangesAsync();
    }

    public async Task<List<ResultBlob>> GetResultBlobsAsync(string historyRecordId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ResultBlobs
            .Where(b => b.HistoryRecordId == historyRecordId)
            .OrderBy(b => b.FileName)
            .ToListAsync();
    }

    public async Task ExportBlobsAsync(string historyRecordId, string targetDir)
    {
        var blobs = await GetResultBlobsAsync(historyRecordId);
        Directory.CreateDirectory(targetDir);
        foreach (var blob in blobs)
        {
            var path = Path.Combine(targetDir, blob.FileName);
            await File.WriteAllBytesAsync(path, blob.Data);
        }
    }
}
