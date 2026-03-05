using TradingJournal.Shared.Common;

namespace TradingJournal.Modules.Trades.Features.V1.Dashboard;

public sealed class GetProfitTrajectory
{
    internal sealed record Request(ProfitTrajectoryFilter Filter) : IQuery<Result<IReadOnlyCollection<ProfitTrajectoryViewModel>>>; 

    internal sealed record ProfitTrajectoryViewModel(DateTime Date, double PnL); 

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Filter)
            .Cascade(CascadeMode.Stop)
            .IsInEnum()
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
            .WithMessage("Invalid filter value. Allowed values are: OneWeek, OneMonth, ThreeMonths, AllTime.");
        }
    }

    internal sealed class Handler(ITradeDbContext context) : IQueryHandler<Request, Result<IReadOnlyCollection<ProfitTrajectoryViewModel>>>
    {
        public async Task<Result<IReadOnlyCollection<ProfitTrajectoryViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            int userId = 0; // TODO: Get user ID from context

            DateTime fromDate = request.Filter switch
            {
                ProfitTrajectoryFilter.OneWeek => DateTime.UtcNow.AddDays(-7),
                ProfitTrajectoryFilter.OneMonth => DateTime.UtcNow.AddMonths(-1),
                ProfitTrajectoryFilter.ThreeMonths => DateTime.UtcNow.AddMonths(-3),
                ProfitTrajectoryFilter.AllTime => DateTime.MinValue,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Filter), "Invalid filter value.")
            };

            List<TradeHistory>? trades = await context.TradeHistories
                .AsNoTracking()
                .Where(t => t.CreatedBy == userId && t.Status == TradeStatus.Closed && t.ClosedDate != null && t.ClosedDate.Value >= fromDate)
                .ToListAsync(cancellationToken);

            if (trades is null || trades.Count == 0)
            {
                return Result<IReadOnlyCollection<ProfitTrajectoryViewModel>>.Failure(Error.NotFound);
            }

            DateTime currentDate = new DateTimeProvider().Now;

            List<ProfitTrajectoryViewModel> trajectory = [];

            for (DateTime date = fromDate.Date; date <= currentDate.Date; date = date.AddDays(1))
            {
                trades.Where(x => x.ClosedDate != null && x.ClosedDate.Value.Date == date.Date && x.Pnl.HasValue)
                .ToList()
                .ForEach(t =>
                {
                    trajectory.Add(new ProfitTrajectoryViewModel(date, t.Pnl.Value));
                });
            }

            return Result<IReadOnlyCollection<ProfitTrajectoryViewModel>>.Success(trajectory);
        }
    }

    public sealed class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/dashboard");

            group.MapGet("/profit-trajectory", async (ProfitTrajectoryFilter filter, IMediator sender) =>
            {
                Result<IReadOnlyCollection<ProfitTrajectoryViewModel>> result = await sender.Send(new Request(filter));

                return result.IsSuccess ? Results.Ok(result) : Results.Problem(result.Errors[0].Description);
            })
             .Produces<Result<IReadOnlyCollection<ProfitTrajectoryViewModel>>>(StatusCodes.Status200OK)
             .Produces(StatusCodes.Status400BadRequest)
             .Produces(StatusCodes.Status500InternalServerError)
             .WithSummary("Get profit trajectory.")
             .WithDescription("Retrieves the profit trajectory based on the specified filter.")
             .WithTags(Tags.Dashboard);
        }
    }
}