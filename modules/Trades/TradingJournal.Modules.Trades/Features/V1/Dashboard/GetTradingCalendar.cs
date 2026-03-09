using TradingJournal.Shared.Common;


namespace TradingJournal.Modules.Trades.Features.V1.Dashboard;

public sealed class GetTradingCalendar
{
    internal sealed record Request(int Month, int Year, DashboardFilter Filter) : IQuery<Result<IReadOnlyCollection<TradingCalendarViewModel>>>;

    internal sealed record TradingCalendarViewModel(DateTime Date, double PnL);

    internal sealed class Handler(ITradeDbContext context) : IQueryHandler<Request, Result<IReadOnlyCollection<TradingCalendarViewModel>>>
    {
        public async Task<Result<IReadOnlyCollection<TradingCalendarViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            int userId = 0; // TODO: Get user ID from context

            DateTime filterFromDate = DashboardFilterHelper.GetFromDate(request.Filter);

            List<TradeHistory>? trades = await context.TradeHistories
                .AsNoTracking()
                .Where(t => t.CreatedBy == userId && t.Status == TradeStatus.Closed && t.ClosedDate != null && 
                    t.ClosedDate.Value.Month == request.Month && t.ClosedDate.Value.Year == request.Year &&
                    t.ClosedDate.Value >= filterFromDate)
                .ToListAsync(cancellationToken);

            if (trades is null || trades.Count == 0)
            {
                return Result<IReadOnlyCollection<TradingCalendarViewModel>>.Failure(Error.NotFound);
            }

            List<TradingCalendarViewModel> calendars = [];

            DateTime CurrentTime = new DateTimeProvider().Now;

            DateTime firstDayOfMonth = new(request.Year, request.Month, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            for (DateTime date = firstDayOfMonth; date <= lastDayOfMonth; date = date.AddDays(1))
            {
                trades.Where(x => x.ClosedDate != null && x.ClosedDate.Value.Date == date.Date)
                .ToList()
                .ForEach(t =>
                {
                    calendars.Add(new TradingCalendarViewModel(date, t.Pnl ?? 0));
                });
            }

            return Result<IReadOnlyCollection<TradingCalendarViewModel>>.Success(calendars);
        }
    }

    public sealed class Endpoint() : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/dashboard");

            group.MapGet("/calendar", async (int month, int year, DashboardFilter filter, IMediator sender) =>
            {
                Result<IReadOnlyCollection<TradingCalendarViewModel>> result = await sender.Send(new Request(month, year, filter));

                return result.IsSuccess ? Results.Ok(result) : Results.Problem(result.Errors[0].Description);
            })
             .Produces<Result<IReadOnlyCollection<TradingCalendarViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get trading calendar for a specific month and year.")
            .WithDescription("Retrieves the trading calendar with PnL for each day in the specified month and year.")
            .WithTags(Tags.Dashboard);
        }
    }
}