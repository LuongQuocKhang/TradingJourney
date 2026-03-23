using TradingJournal.Modules.Strategies.Services;

namespace TradingJournal.Modules.Strategies.Features.V1.Backtests;

public sealed class DownloadHistoricalData
{
    public record Request(
        string Asset,
        DateTime StartDate,
        DateTime EndDate) : ICommand<Result<DownloadResult>>;

    public record DownloadResult(string FilePath, int CandleCount);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Asset)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Asset ticker symbol is required (e.g. 'AAPL', 'BTCUSD').");

            RuleFor(x => x.EndDate)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(x => x.StartDate).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("End date must be after start date.");
        }
    }

    internal sealed class Handler(IHistoricalDataService dataService)
        : ICommandHandler<Request, Result<DownloadResult>>
    {
        public async Task<Result<DownloadResult>> Handle(Request request, CancellationToken cancellationToken)
        {
            try
            {
                (string filePath, int candleCount) = await dataService.DownloadAndSaveAsync(
                    request.Asset, request.StartDate, request.EndDate, cancellationToken);

                return Result<DownloadResult>.Success(new DownloadResult(filePath, candleCount));
            }
            catch (Exception ex)
            {
                return Result<DownloadResult>.Failure(Error.Create(ex.Message));
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/backtests");

            group.MapPost("/historical-data", async ([FromBody] Request request, ISender sender) =>
            {
                Result<DownloadResult> result = await sender.Send(request);

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<DownloadResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Download historical market data.")
            .WithDescription("Downloads OHLCV data from Yahoo Finance and saves to an Excel file for backtesting.")
            .WithTags(Tags.Backtest);
        }
    }
}
