using TradingJournal.Modules.Strategies.Services;

namespace TradingJournal.Modules.Strategies.Features.V1.Backtests;

public sealed class GetHistoricalDataFiles
{
    internal sealed class Request : IQuery<Result<List<HistoricalDataFileInfo>>>
    {
    }

    internal sealed class Handler(IHistoricalDataService dataService)
        : IQueryHandler<Request, Result<List<HistoricalDataFileInfo>>>
    {
        public async Task<Result<List<HistoricalDataFileInfo>>> Handle(Request request, CancellationToken cancellationToken)
        {
            List<HistoricalDataFileInfo> files = await dataService.GetAvailableFilesAsync(cancellationToken);

            return Result<List<HistoricalDataFileInfo>>.Success(files);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/backtests");

            group.MapGet("/historical-data", async (ISender sender) =>
            {
                Result<List<HistoricalDataFileInfo>> result = await sender.Send(new Request());

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<List<HistoricalDataFileInfo>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("List available historical data files.")
            .WithDescription("Lists all downloaded Excel files with metadata (asset, date range, file size).")
            .WithTags(Tags.Backtest);
        }
    }
}
