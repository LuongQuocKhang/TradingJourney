using TradingJournal.Modules.Strategies.Dtos;

namespace TradingJournal.Modules.Strategies.Services;

public interface IHistoricalDataService
{
    /// <summary>
    /// Downloads OHLCV data from Yahoo Finance and saves to an Excel file.
    /// Returns the file path and number of candles saved.
    /// </summary>
    Task<(string FilePath, int CandleCount)> DownloadAndSaveAsync(string asset, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads candle data from an Excel file matching the asset and date range.
    /// </summary>
    Task<List<CandleDto>> LoadCandlesAsync(string asset, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available historical data Excel files.
    /// </summary>
    Task<List<HistoricalDataFileInfo>> GetAvailableFilesAsync(CancellationToken cancellationToken = default);
}

public class HistoricalDataFileInfo
{
    public string FileName { get; set; } = string.Empty;

    public string Asset { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public long FileSizeBytes { get; set; }
}
