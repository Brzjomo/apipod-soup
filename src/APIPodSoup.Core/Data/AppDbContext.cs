using APIPodSoup.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace APIPodSoup.Core.Data;

public class AppDbContext : DbContext
{
    public DbSet<HistoryRecord> HistoryRecords => Set<HistoryRecord>();
    public DbSet<ResultBlob> ResultBlobs => Set<ResultBlob>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistoryRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TaskId);
        });

        modelBuilder.Entity<ResultBlob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.HistoryRecordId);
        });
    }
}
