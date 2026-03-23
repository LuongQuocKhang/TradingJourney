using MassTransit;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Messaging.Shared;
using TradingJournal.Modules.Strategies.Common.Enums;
using TradingJournal.Modules.Strategies.Domain;
using TradingJournal.Modules.Strategies.Dtos;
using TradingJournal.Modules.Strategies.Infrastructure;
using TradingJournal.Modules.Strategies.Services;

namespace TradingJournal.Jobs.BackTestEngine.Consumers;

public class RunBacktestEventConsumer(IStrategyDbContext context, IHistoricalDataService dataService, ILogger<RunBacktestEventConsumer> logger) : IConsumer<RunBacktestEvent>
{
    public async Task Consume(ConsumeContext<RunBacktestEvent> messageContext)
    {
        int backtestId = messageContext.Message.BacktestId;

        try
        {
            await context.BeginTransaction();

            Backtest? backtest = await context.Backtests
                .Include(b => b.Strategy)
                .Include(b => b.BacktestTrades)
                .FirstOrDefaultAsync(b => b.Id == backtestId, messageContext.CancellationToken);

            if (backtest is null)
            {
                logger.LogWarning("Backtest {BacktestId} not found.", backtestId);
                await context.RollbackTransaction();
                return;
            }

            if (backtest.Strategy is null)
            {
                logger.LogWarning("Strategy not found for Backtest {BacktestId}.", backtestId);
                await context.RollbackTransaction();
                return;
            }

            // Clear any previous trades if re-running
            if (backtest.BacktestTrades.Count > 0)
            {
                context.BacktestTrades.RemoveRange(backtest.BacktestTrades);
                await context.SaveChangesAsync(messageContext.CancellationToken);
            }

            Strategy strategy = backtest.Strategy;

            // Load historical candle data from Excel
            List<CandleDto> candles;
            try
            {
                candles = await dataService.LoadCandlesAsync(strategy.Asset, backtest.StartDate, backtest.EndDate, messageContext.CancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load historical data for Backtest {BacktestId}", backtestId);
                backtest.Status = BacktestStatus.Failed;
                await context.SaveChangesAsync(messageContext.CancellationToken);
                await context.CommitTransaction();
                return;
            }

            if (candles.Count == 0)
            {
                logger.LogWarning("No historical data found for {Asset} in the specified date range. BacktestId: {BacktestId}", strategy.Asset, backtestId);
                backtest.Status = BacktestStatus.Failed;
                await context.SaveChangesAsync(messageContext.CancellationToken);
                await context.CommitTransaction();
                return;
            }

            // Simulate trades applying strategy rules to candle data
            List<BacktestTrade> simulatedTrades = SimulateTrades(backtest, strategy, candles);

            if (simulatedTrades.Count > 0)
            {
                await context.BacktestTrades.AddRangeAsync(simulatedTrades, messageContext.CancellationToken);
            }

            // Compute aggregate metrics
            ComputeMetrics(backtest, simulatedTrades);

            backtest.Status = BacktestStatus.Completed;
            await context.SaveChangesAsync(messageContext.CancellationToken);
            await context.CommitTransaction();

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing Backtest {BacktestId}", backtestId);
            await context.RollbackTransaction();

            // Try to mark as failed
            try
            {
                Backtest? bt = await context.Backtests.FindAsync([backtestId], messageContext.CancellationToken);
                if (bt is not null)
                {
                    bt.Status = BacktestStatus.Failed;
                    await context.SaveChangesAsync(messageContext.CancellationToken);
                }
            }
            catch { /* ignore */ }
        }
    }

    private static List<BacktestTrade> SimulateTrades(Backtest backtest, Strategy strategy, List<CandleDto> candles)
    {
        List<BacktestTrade> trades = [];
        double currentCapital = backtest.InitialCapital;

        // Simple simulation: We enter a Long trade on every candle Open and exit when high/low hits TP/SL.
        bool isLong = true;

        for (int i = 0; i < candles.Count; i++)
        {
            CandleDto candle = candles[i];
            double entryPrice = candle.Open;

            double stopLoss = ComputeStopLoss(strategy, entryPrice, isLong);
            double takeProfit = ComputeTakeProfit(strategy, entryPrice, stopLoss, isLong);

            double exitPrice = 0;
            string? notes = null;
            DateTime? exitDate = null;

            // Walk forward to find exit
            for (int j = i; j < candles.Count; j++)
            {
                CandleDto futureCandle = candles[j];

                if (futureCandle.Low <= stopLoss)
                {
                    exitPrice = stopLoss;
                    notes = "Stop loss hit";
                    exitDate = futureCandle.Date;
                    break;
                }

                if (futureCandle.High >= takeProfit)
                {
                    exitPrice = takeProfit;
                    notes = "Take profit hit";
                    exitDate = futureCandle.Date;
                    break;
                }

                if (j == candles.Count - 1)
                {
                    exitPrice = futureCandle.Close;
                    notes = "Closed at end of backtest period";
                    exitDate = futureCandle.Date;
                    break;
                }
            }

            if (!exitDate.HasValue) continue;

            double pnlMultiplier = isLong
                ? exitPrice - entryPrice
                : entryPrice - exitPrice;

            double positionSize = ComputePositionSize(strategy, currentCapital, entryPrice);
            double pnl = Math.Round(pnlMultiplier * positionSize, 2);

            currentCapital += pnl;

            trades.Add(new BacktestTrade
            {
                Id = 0,
                BacktestId = backtest.Id,
                Asset = strategy.Asset,
                Position = isLong ? 0 : 1, // Long
                EntryPrice = Math.Round(entryPrice, 6),
                ExitPrice = Math.Round(exitPrice, 6),
                StopLoss = Math.Round(stopLoss, 6),
                TakeProfit = Math.Round(takeProfit, 6),
                EntryDate = candle.Date,
                ExitDate = exitDate,
                Pnl = pnl,
                Notes = notes
            });
        }

        return trades;
    }

    private static double ComputeStopLoss(Strategy strategy, double entryPrice, bool isLong)
    {
        return strategy.StopLossType switch
        {
            StopLossType.Fixed => isLong
                ? entryPrice - strategy.StopLossValue
                : entryPrice + strategy.StopLossValue,
            StopLossType.Percent => isLong
                ? entryPrice * (1 - strategy.StopLossValue / 100)
                : entryPrice * (1 + strategy.StopLossValue / 100),
            StopLossType.Atr => isLong
                ? entryPrice - strategy.StopLossValue
                : entryPrice + strategy.StopLossValue,
            _ => isLong
                ? entryPrice - strategy.StopLossValue
                : entryPrice + strategy.StopLossValue
        };
    }

    private static double ComputeTakeProfit(Strategy strategy, double entryPrice, double stopLoss, bool isLong)
    {
        return strategy.TakeProfitType switch
        {
            TakeProfitType.Fixed => isLong
                ? entryPrice + strategy.TakeProfitValue
                : entryPrice - strategy.TakeProfitValue,
            TakeProfitType.Percent => isLong
                ? entryPrice * (1 + strategy.TakeProfitValue / 100)
                : entryPrice * (1 - strategy.TakeProfitValue / 100),
            TakeProfitType.RrRatio => isLong
                ? entryPrice + Math.Abs(entryPrice - stopLoss) * strategy.TakeProfitValue
                : entryPrice - Math.Abs(entryPrice - stopLoss) * strategy.TakeProfitValue,
            _ => isLong
                ? entryPrice + strategy.TakeProfitValue
                : entryPrice - strategy.TakeProfitValue
        };
    }

    private static double ComputePositionSize(Strategy strategy, double capital, double entryPrice)
    {
        return strategy.PositionSizing switch
        {
            PositionSizingType.Fixed => strategy.PositionSizeValue,
            PositionSizingType.PercentEquity => (capital * strategy.PositionSizeValue / 100) / entryPrice,
            _ => strategy.PositionSizeValue
        };
    }

    private static void ComputeMetrics(Backtest backtest, List<BacktestTrade> trades)
    {
        if (trades.Count == 0)
        {
            backtest.TotalTrades = 0;
            backtest.WinCount = 0;
            backtest.LossCount = 0;
            backtest.WinRate = 0;
            backtest.TotalPnl = 0;
            backtest.FinalCapital = backtest.InitialCapital;
            return;
        }

        List<BacktestTrade> wins = [.. trades.Where(t => t.Pnl > 0)];
        List<BacktestTrade> losses = [.. trades.Where(t => t.Pnl <= 0)];

        backtest.TotalTrades = trades.Count;
        backtest.WinCount = wins.Count;
        backtest.LossCount = losses.Count;
        backtest.WinRate = Math.Round((double)wins.Count / trades.Count * 100, 1);
        backtest.TotalPnl = Math.Round(trades.Sum(t => t.Pnl), 2);
        backtest.FinalCapital = Math.Round(backtest.InitialCapital + backtest.TotalPnl, 2);

        backtest.AvgWin = wins.Count > 0 ? Math.Round(wins.Average(t => t.Pnl), 2) : 0;
        backtest.AvgLoss = losses.Count > 0 ? Math.Round(Math.Abs(losses.Average(t => t.Pnl)), 2) : 0;
        backtest.LargestWin = wins.Count > 0 ? Math.Round(wins.Max(t => t.Pnl), 2) : 0;
        backtest.LargestLoss = losses.Count > 0 ? Math.Round(losses.Min(t => t.Pnl), 2) : 0;

        double grossProfit = wins.Sum(t => t.Pnl);
        double grossLoss = Math.Abs(losses.Sum(t => t.Pnl));
        backtest.ProfitFactor = grossLoss > 0
            ? Math.Round(grossProfit / grossLoss, 2)
            : (grossProfit > 0 ? double.MaxValue : 0);

        double peak = 0, equity = 0, maxDD = 0, maxDDPct = 0;
        foreach (BacktestTrade t in trades.OrderBy(t => t.EntryDate))
        {
            equity += t.Pnl;
            if (equity > peak) peak = equity;
            double dd = peak - equity;
            if (dd > maxDD)
            {
                maxDD = dd;
                maxDDPct = peak > 0 ? dd / peak * 100 : 0;
            }
        }
        backtest.MaxDrawdown = Math.Round(maxDD, 2);
        backtest.MaxDrawdownPct = Math.Round(maxDDPct, 1);

        double[] returns = [.. trades.OrderBy(t => t.EntryDate).Select(t => t.Pnl)];
        double meanReturn = returns.Average();
        double stdDev = returns.Length > 1
            ? Math.Sqrt(returns.Sum(r => Math.Pow(r - meanReturn, 2)) / (returns.Length - 1))
            : 0;
        backtest.SharpeRatio = stdDev > 0
            ? Math.Round(meanReturn / stdDev * Math.Sqrt(252), 2)
            : 0;
    }
}
