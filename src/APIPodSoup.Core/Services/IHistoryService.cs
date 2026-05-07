using APIPodSoup.Core.Models;

namespace APIPodSoup.Core.Services;

public interface IHistoryService
{
    Task<List<HistoryRecord>> GetAllAsync(int skip = 0, int take = 20);
    Task<HistoryRecord?> GetByIdAsync(string id);
    Task<HistoryRecord> CreateAsync(HistoryRecord record);
    Task UpdateAsync(HistoryRecord record);
    Task DeleteAsync(string id);
    Task<int> GetTotalCountAsync();
    Task SaveResultBlobsAsync(string historyRecordId, List<ResultBlob> blobs);
    Task<List<ResultBlob>> GetResultBlobsAsync(string historyRecordId);
    Task ExportBlobsAsync(string historyRecordId, string targetDir);
}
