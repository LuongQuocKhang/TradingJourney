using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TradingJournal.Modules.Trades.Dto;
using TradingJournal.Modules.Trades.Extensions;
using TradingJournal.Modules.Trades.Options;
using TradingJournal.Shared.Dtos;

namespace TradingJournal.Modules.Trades.Services;

internal sealed class GoogleGenAIService(IPromptService promptService, ITradeDbContext context, IEmotionTagProvider emotionTagProvider,
    IPsychologyProvider psychologyProvider,
    Client googleGenAiClient,
    IImageHelper imageHelper,
    IOptions<GoogleGenAIOptions> options) : IGoogleGenAIService
{
    public async Task<TradeAnalysisResultDto?> GenerateTradingOrderSummary(int tradeHistoryId, CancellationToken cancellationToken)
    {
        string tradingOrderSummaryPrompt = await promptService.GetTradingOrderSummary();

        if (string.IsNullOrEmpty(tradingOrderSummaryPrompt))
        {
            throw new InvalidOperationException("Not Found Prompt File.");
        }

        TradeHistory tradeHistory = await context.TradeHistories
            .Include(th => th.TradeScreenShots)
            .Include(th => th.TradeEmotionTags)
            .Include(th => th.TradeChecklists)
            .ThenInclude(th => th.PretradeChecklist)
            .Include(th => th.TradeTechnicalAnalysisTags)
            .ThenInclude(th => th.TechnicalAnalysis)
            .FirstOrDefaultAsync(th => th.Id == tradeHistoryId, cancellationToken) ?? throw new InvalidOperationException("Trade history not found.");

        TradingZone tradingZone = await context.TradingZones.FirstOrDefaultAsync(tz => tz.Id == tradeHistory.TradingZoneId, cancellationToken)
            ?? throw new InvalidOperationException("Trading zone not found.");

        List<string> technicalTagNames = [.. tradeHistory.TradeTechnicalAnalysisTags.Select(ttat => ttat.TechnicalAnalysis?.Name ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))];

        List<EmotionTagCacheDto> emotionTags = await emotionTagProvider.GetEmotionTagsAsync(cancellationToken);

        HashSet<int> emotionTagIds = [.. tradeHistory.TradeEmotionTags?.Select(tet => tet.EmotionTagId) ?? []];

        List<string> tradeEmotionalTags = [.. emotionTags.Where(x => emotionTagIds.Contains(x.Id)).Select(x => x.Name)];

        HashSet<int> pretradeCheckListIds = [.. tradeHistory.TradeChecklists.Select(x => x.PretradeChecklistId)];

        List<string> checkListNames = await context.PretradeChecklists
            .Where(ptc => pretradeCheckListIds.Contains(ptc.Id))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken: cancellationToken);

        List<string> psychologys = await psychologyProvider.GetPsychologyByDate(tradeHistory.Date, cancellationToken);

        TradeSumamryDto tradeSumamry = new(
            Asset: tradeHistory.Asset,
            EntryPrice: tradeHistory.EntryPrice,
            Position: tradeHistory.Position.ToString(),
            TargetTier1: tradeHistory.TargetTier1,
            TargetTier2: tradeHistory.TargetTier2,
            TargetTier3: tradeHistory.TargetTier3,
            StopLoss: tradeHistory.StopLoss,
            Notes: tradeHistory.Notes ?? string.Empty,
            ExitPrice: tradeHistory.ExitPrice,
            Pnl: tradeHistory.Pnl,
            TradeTechnicalAnalysisTags: technicalTagNames,
            EmotionTags: tradeEmotionalTags,
            ConfidenceLevel: tradeHistory.ConfidenceLevel.ToString(),
            TradeHistoryChecklists: checkListNames,
            TradingZone: tradingZone.Name,
            OpenDate: tradeHistory.Date,
            ClosedDate: tradeHistory.ClosedDate ?? DateTime.Now
        );

        List<byte[]> imageContents = await imageHelper.GetImageBytesFromUrls(tradeHistory.TradeScreenShots.Select(tss => tss.Url).ToList(), cancellationToken);

        string finalPrompt = tradingOrderSummaryPrompt.Replace("{{Asset}}", tradeSumamry.Asset)
            .Replace("{{Position}}", tradeSumamry.Position)
            .Replace("{{EntryPrice}}", tradeSumamry.EntryPrice.ToString())
            .Replace("{{TargetTier1}}", tradeSumamry.TargetTier1.ToString())
            .Replace("{{TargetTier2}}", tradeSumamry.TargetTier2?.ToString() ?? string.Empty)
            .Replace("{{TargetTier3}}", tradeSumamry.TargetTier3?.ToString() ?? string.Empty)
            .Replace("{{StopLoss}}", tradeSumamry.StopLoss.ToString())
            .Replace("{{Notes}}", tradeSumamry.Notes)
            .Replace("{{ExitPrice}}", tradeSumamry.ExitPrice?.ToString() ?? string.Empty)
            .Replace("{{Pnl}}", tradeSumamry.Pnl?.ToString() ?? string.Empty)
            .Replace("{{ConfidenceLevel}}", tradeSumamry.ConfidenceLevel)
            .Replace("{{TradingZone}}", tradeSumamry.TradingZone)
            .Replace("{{Date}}", tradeSumamry.OpenDate.ToShortDateString())
            .Replace("{{ClosedDate}}", tradeSumamry.ClosedDate.ToShortDateString())
            .Replace("{{TradingZone}}", tradeSumamry.TradingZone)
            .Replace("{{Notes}}", tradeSumamry.Notes)
            .Replace("{{TradeTechnicalAnalysisTags}}", string.Join(", ", tradeSumamry.TradeTechnicalAnalysisTags ?? []))
            .Replace("{{TradeHistoryChecklists}}", string.Join(", ", tradeSumamry.TradeHistoryChecklists ?? []))
            .Replace("{{EmotionTags}}", string.Join(", ", tradeSumamry.EmotionTags ?? []))
            .Replace("{{PsychologyNotes}}", string.Join(", ", psychologys ?? []))
            ;

        List<Part> imageParts = [.. imageContents.Select((content, index) => new Part
        {
            InlineData = new Blob
            {
                Data = content,
                MimeType = "image/jpeg"
            }
        })];

        List<Part> allPromptParts = [new Part { Text = finalPrompt }];
        allPromptParts.AddRange(imageParts);

        List<Content> contents =
        [
            new()
            {
                Role = "user",
                Parts = allPromptParts
            },
        ];

        List<Tool> tools =
        [
            new Tool { UrlContext = new UrlContext() },
        ];

        GenerateContentConfig config = new()
        {
            ThinkingConfig = new ThinkingConfig
            {
                ThinkingLevel = ThinkingLevel.FromString(options.Value.ThinkingLevel),
            },
            MediaResolution = options.Value.MediaResolution,
            Tools = tools,
            ResponseMimeType = options.Value.ResponseMimeType,
        };

        var response = await googleGenAiClient.Models.GenerateContentAsync(
            model: options.Value.Model ?? "gemini-2.5-pro",
            contents: contents,
            config: config,
            cancellationToken: cancellationToken
        );

        if (string.IsNullOrEmpty(response.Text))
        {
            throw new Exception("Gemini returned an empty response.");
        }

        // Deserialize JSON string thành C# Object
        try
        {
            TradeAnalysisResultDto? result = JsonSerializer.Deserialize<TradeAnalysisResultDto>(response.Text);

            await context.TradingSummaries.AddAsync(new TradingSummary()
            {
                Id = 0,
                TradeId = tradeHistoryId,
                ExecutiveSummary = result?.ExecutiveSummary ?? string.Empty,
                TechnicalInsights = result?.TechnicalInsights ?? string.Empty,
                PsychologyAnalysis = result?.PsychologyAnalysis ?? string.Empty,
                CriticalMistakes = new CriticalMistakes()
                {
                    Psychological = result?.CriticalMistakes?.Psychological ?? [],
                    Technical = result?.CriticalMistakes?.Technical ?? [],
                },
            }, cancellationToken: cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (JsonException ex)
        {
            // Bắt lỗi nếu Gemini trả về JSON không hợp lệ
            throw new Exception($"Failed to parse AI response into TradeAnalysisResult. Raw response: {response.Text}", ex);
        }
    }
}
