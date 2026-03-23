using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using TradingJournal.Modules.Strategies.Dtos;
using YahooFinanceApi;

namespace TradingJournal.Modules.Strategies.Services;

internal sealed class HistoricalDataService(IWebHostEnvironment env) : IHistoricalDataService
{
    private string DataDirectory => Path.Combine(env.ContentRootPath, "wwwroot", "backtest-data");

    public async Task<(string FilePath, int CandleCount)> DownloadAndSaveAsync(
        string asset, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // Fetch from Yahoo Finance
        IReadOnlyList<Candle> history = await Yahoo.GetHistoricalAsync(
            asset, startDate, endDate, Period.Daily);

        if (history == null || history.Count == 0)
            throw new InvalidOperationException($"No historical data found for '{asset}' between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.");

        // Ensure directory exists
        if (!Directory.Exists(DataDirectory))
            Directory.CreateDirectory(DataDirectory);

        // Build file path
        string fileName = $"{asset.ToUpperInvariant()}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
        string filePath = Path.Combine(DataDirectory, fileName);

        // Write Excel file
        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("OHLCV");

        // Header row
        worksheet.Cell(1, 1).Value = "Date";
        worksheet.Cell(1, 2).Value = "Open";
        worksheet.Cell(1, 3).Value = "High";
        worksheet.Cell(1, 4).Value = "Low";
        worksheet.Cell(1, 5).Value = "Close";
        worksheet.Cell(1, 6).Value = "Volume";

        // Style header
        IXLRange headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        int row = 2;
        foreach (Candle candle in history)
        {
            worksheet.Cell(row, 1).Value = candle.DateTime;
            worksheet.Cell(row, 2).Value = (double)candle.Open;
            worksheet.Cell(row, 3).Value = (double)candle.High;
            worksheet.Cell(row, 4).Value = (double)candle.Low;
            worksheet.Cell(row, 5).Value = (double)candle.Close;
            worksheet.Cell(row, 6).Value = (double)candle.Volume;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Save
        await Task.Run(() => workbook.SaveAs(filePath), cancellationToken);

        string relativePath = $"/backtest-data/{fileName}";
        return (relativePath, history.Count);
    }

    public Task<List<CandleDto>> LoadCandlesAsync(
        string asset, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // Find matching file
        string fileName = $"{asset.ToUpperInvariant()}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
        string filePath = Path.Combine(DataDirectory, fileName);

        if (!File.Exists(filePath))
        {
            // Try to find any file for this asset that covers the date range
            filePath = FindBestMatchingFile(asset, startDate, endDate);
            if (filePath == null)
                throw new FileNotFoundException($"No historical data file found for '{asset}'. Download data first using the /api/v1/backtests/historical-data endpoint.");
        }

        List<CandleDto> candles = [];

        using XLWorkbook workbook = new(filePath);
        IXLWorksheet worksheet = workbook.Worksheet(1);

        // Skip header row, read data
        foreach (IXLRow row in worksheet.RowsUsed().Skip(1))
        {
            try
            {
                DateTime date = row.Cell(1).GetDateTime();

                // Filter to requested date range
                if (date < startDate || date > endDate)
                    continue;

                candles.Add(new CandleDto
                {
                    Date = date,
                    Open = row.Cell(2).GetDouble(),
                    High = row.Cell(3).GetDouble(),
                    Low = row.Cell(4).GetDouble(),
                    Close = row.Cell(5).GetDouble(),
                    Volume = (long)row.Cell(6).GetDouble()
                });
            }
            catch
            {
                // Skip malformed rows
            }
        }

        return Task.FromResult(candles.OrderBy(c => c.Date).ToList());
    }

    public Task<List<HistoricalDataFileInfo>> GetAvailableFilesAsync(CancellationToken cancellationToken = default)
    {
        List<HistoricalDataFileInfo> files = [];

        if (!Directory.Exists(DataDirectory))
            return Task.FromResult(files);

        foreach (string filePath in Directory.GetFiles(DataDirectory, "*.xlsx"))
        {
            FileInfo fileInfo = new(filePath);
            string name = Path.GetFileNameWithoutExtension(filePath);

            // Parse filename: ASSET_YYYYMMDD_YYYYMMDD
            string[] parts = name.Split('_');
            if (parts.Length >= 3)
            {
                string asset = parts[0];
                if (DateTime.TryParseExact(parts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime start) &&
                    DateTime.TryParseExact(parts[2], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime end))
                {
                    files.Add(new HistoricalDataFileInfo
                    {
                        FileName = fileInfo.Name,
                        Asset = asset,
                        StartDate = start,
                        EndDate = end,
                        FileSizeBytes = fileInfo.Length
                    });
                }
            }
        }

        return Task.FromResult(files.OrderByDescending(f => f.EndDate).ToList());
    }

    private string? FindBestMatchingFile(string asset, DateTime startDate, DateTime endDate)
    {
        if (!Directory.Exists(DataDirectory))
            return null;

        string prefix = asset.ToUpperInvariant() + "_";

        foreach (string filePath in Directory.GetFiles(DataDirectory, $"{prefix}*.xlsx"))
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            string[] parts = name.Split('_');

            if (parts.Length >= 3 &&
                DateTime.TryParseExact(parts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fileStart) &&
                DateTime.TryParseExact(parts[2], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fileEnd))
            {
                // File covers at least part of the requested range
                if (fileStart <= endDate && fileEnd >= startDate)
                    return filePath;
            }
        }

        return null;
    }
}
