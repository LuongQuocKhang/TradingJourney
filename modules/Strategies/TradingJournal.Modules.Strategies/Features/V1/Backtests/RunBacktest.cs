using TradingJournal.Modules.Strategies.Dtos;

namespace TradingJournal.Modules.Strategies.Features.V1.Backtests;

public sealed class RunBacktest
{
    internal sealed record Request(int Id) : ICommand<Result<object>>;

    internal sealed class Handler(IStrategyDbContext context)
        : ICommandHandler<Request, Result<object>>
    {
        public async Task<Result<object>> Handle(Request request, CancellationToken cancellationToken)
        {
            try
            {
                await context.BeginTransaction();

                Backtest? backtest = await context.Backtests
                    .Include(b => b.Strategy)
                    .Include(b => b.BacktestTrades)
                    .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

                if (backtest is null)
                    return Result<object>.Failure(Error.Create("Backtest not found."));

                if (backtest.Strategy is null)
                    return Result<object>.Failure(Error.Create("Associated strategy not found."));

                // Mark as running
                backtest.Status = BacktestStatus.Running;
                await context.SaveChangesAsync(cancellationToken);

                // Publish event to Background Worker
                //await publishEndpoint.Publish(new RunBacktestEvent { BacktestId = backtest.Id }, cancellationToken);

                await context.CommitTransaction();

                // Build response
                return Result<object>.Success(new { Message = "Backtest started successfully." });
            }
            catch (Exception ex)
            {
                await context.RollbackTransaction();

                // Try to mark as failed
                try
                {
                    Backtest? bt = await context.Backtests.FindAsync([request.Id], cancellationToken);
                    if (bt is not null)
                    {
                        bt.Status = BacktestStatus.Failed;
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }
                catch { /* ignore */ }

                return Result<object>.Failure(Error.Create(ex.Message));
            }
        }

        /// <summary>
        /// Simulates trades by walking through historical candle data and applying the strategy's SL/TP rules.
        /// </summary>
        private static List<BacktestTrade> SimulateTrades(Backtest backtest, Strategy strategy, List<CandleDto> candles)
        {
            List<BacktestTrade> trades = [];
            double currentCapital = backtest.InitialCapital;

            // Simple simulation: We enter a Long trade on every candle Open and exit when high/low hits TP/SL.
            // (If strategy was more complex, we'd check indicators). We'll assume Long only for stock data.
            bool isLong = true; 

            for (int i = 0; i < candles.Count; i++)
            {
                CandleDto candle = candles[i];
                double entryPrice = candle.Open;

                // Compute stop loss based on strategy rules
                double stopLoss = ComputeStopLoss(strategy, entryPrice, isLong);

                // Compute take profit based on strategy rules
                double takeProfit = ComputeTakeProfit(strategy, entryPrice, stopLoss, isLong);

                double exitPrice = 0;
                string? notes = null;
                DateTime? exitDate = null;

                // Walk forward to find exit
                for (int j = i; j < candles.Count; j++)
                {
                    CandleDto futureCandle = candles[j];

                    // Did we hit Stop Loss?
                    if (futureCandle.Low <= stopLoss)
                    {
                        exitPrice = stopLoss;
                        notes = "Stop loss hit";
                        exitDate = futureCandle.Date;
                        break; // Exit trade
                    }

                    // Did we hit Take Profit?
                    if (futureCandle.High >= takeProfit)
                    {
                        exitPrice = takeProfit;
                        notes = "Take profit hit";
                        exitDate = futureCandle.Date;
                        break; // Exit trade
                    }

                    // If it's the last candle of the backtest, close at End of Day
                    if (j == candles.Count - 1)
                    {
                        exitPrice = futureCandle.Close;
                        notes = "Closed at end of backtest period";
                        exitDate = futureCandle.Date;
                        break; // Exit trade
                    }
                }

                if (!exitDate.HasValue) continue; // Should not happen given logic above

                // Calculate PnL multiplier
                double pnlMultiplier = isLong
                    ? exitPrice - entryPrice
                    : entryPrice - exitPrice;
                
                // For stocks, assuming 1 unit of Asset if Fixed. If % Equity, depends on strategy.
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
                PositionSizingType.Fixed => strategy.PositionSizeValue, // e.g., 100 shares
                PositionSizingType.PercentEquity => (capital * strategy.PositionSizeValue / 100) / entryPrice, // e.g., 2% of $10000 / $150 = 1.33 shares
                _ => strategy.PositionSizeValue
            };
        }

        /// <summary>
        /// Computes aggregate performance metrics from simulated trades.
        /// </summary>
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

            List<BacktestTrade> wins = trades.Where(t => t.Pnl > 0).ToList();
            List<BacktestTrade> losses = trades.Where(t => t.Pnl <= 0).ToList();

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

            // Profit Factor
            double grossProfit = wins.Sum(t => t.Pnl);
            double grossLoss = Math.Abs(losses.Sum(t => t.Pnl));
            backtest.ProfitFactor = grossLoss > 0
                ? Math.Round(grossProfit / grossLoss, 2)
                : (grossProfit > 0 ? double.MaxValue : 0);

            // Max Drawdown
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

            // Sharpe Ratio (simplified)
            double[] returns = trades.OrderBy(t => t.EntryDate).Select(t => t.Pnl).ToArray();
            double meanReturn = returns.Average();
            double stdDev = returns.Length > 1
                ? Math.Sqrt(returns.Sum(r => Math.Pow(r - meanReturn, 2)) / (returns.Length - 1))
                : 0;
            backtest.SharpeRatio = stdDev > 0
                ? Math.Round(meanReturn / stdDev * Math.Sqrt(252), 2)
                : 0;
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/backtests");

            group.MapPost("/{id:int}/run", async (int id, ISender sender) =>
            {
                Result<object> result = await sender.Send(new Request(id));

                return result.IsSuccess
                    ? Results.Accepted()
                    : Results.BadRequest(result);
            })
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Run a backtest.")
            .WithDescription("Executes the backtest engine against historical candle data using the linked strategy's rules.")
            .WithTags(Tags.Backtest);
        }
    }
}
