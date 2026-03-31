using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TradingJournal.Modules.Trades.Dto;
using TradingJournal.Modules.Trades.Extensions;
using TradingJournal.Modules.Trades.Options;
using TradingJournal.Shared.Dtos;

namespace TradingJournal.Modules.Trades.Services;

internal sealed class GoogleGenAIService(
    IPromptService promptService,
    ITradeDbContext context,
    IEmotionTagProvider emotionTagProvider,
    IPsychologyProvider psychologyProvider,
    Client googleGenAiClient,
    IImageHelper imageHelper,
    IOptions<GoogleGenAIOptions> options) : IGoogleGenAIService
{
    public async Task<TradeAnalysisResultDto?> GenerateTradingOrderSummary(int tradeHistoryId, CancellationToken cancellationToken)
    {
        string promptTemplate = await promptService.GetTradingOrderSummary();

        if (string.IsNullOrEmpty(promptTemplate))
        {
            throw new InvalidOperationException("Not Found Prompt File.");
        }

        TradeHistory tradeHistory = await LoadTradeHistory(tradeHistoryId, cancellationToken);

        (TradeSumamryDto tradeSummary, List<string> psychologyNotes) = await BuildTradeSummaryDto(tradeHistory, cancellationToken);

        string finalPrompt = BuildPrompt(promptTemplate, tradeSummary, psychologyNotes);

        List<byte[]> imageContents = await imageHelper.GetImageBytesFromUrls(
            tradeHistory.TradeScreenShots.Select(tss => tss.Url).ToList(),
            cancellationToken);

        GenerateContentResponse response = await SendGeminiRequest(finalPrompt, imageContents, cancellationToken);

        return ParseAiResponse(response);
    }

    private async Task<TradeHistory> LoadTradeHistory(int tradeHistoryId, CancellationToken cancellationToken)
    {
        return await context.TradeHistories
            .Include(th => th.TradeScreenShots)
            .Include(th => th.TradeEmotionTags)
            .Include(th => th.TradeChecklists)
                .ThenInclude(th => th.PretradeChecklist)
            .Include(th => th.TradeTechnicalAnalysisTags)
                .ThenInclude(th => th.TechnicalAnalysis)
            .FirstOrDefaultAsync(th => th.Id == tradeHistoryId, cancellationToken)
            ?? throw new InvalidOperationException("Trade history not found.");
    }

    private async Task<(TradeSumamryDto Summary, List<string> PsychologyNotes)> BuildTradeSummaryDto(
        TradeHistory tradeHistory, CancellationToken cancellationToken)
    {
        TradingZone tradingZone = await context.TradingZones
            .FirstOrDefaultAsync(tz => tz.Id == tradeHistory.TradingZoneId, cancellationToken)
            ?? throw new InvalidOperationException("Trading zone not found.");

        List<string> technicalTagNames = [.. tradeHistory.TradeTechnicalAnalysisTags
            .Select(ttat => ttat.TechnicalAnalysis?.Name ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))];

        List<string> tradeEmotionalTags = await GetEmotionTagNames(tradeHistory, cancellationToken);

        List<string> checkListNames = await GetChecklistNames(tradeHistory, cancellationToken);

        List<string> psychologyNotes = await psychologyProvider.GetPsychologyByDate(tradeHistory.Date, cancellationToken);

        TradeSumamryDto summary = new(
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

        return (summary, psychologyNotes);
    }

    private async Task<List<string>> GetEmotionTagNames(TradeHistory tradeHistory, CancellationToken cancellationToken)
    {
        List<EmotionTagCacheDto> emotionTags = await emotionTagProvider.GetEmotionTagsAsync(cancellationToken);
        HashSet<int> emotionTagIds = [.. tradeHistory.TradeEmotionTags?.Select(tet => tet.EmotionTagId) ?? []];

        return [.. emotionTags.Where(x => emotionTagIds.Contains(x.Id)).Select(x => x.Name)];
    }

    private async Task<List<string>> GetChecklistNames(TradeHistory tradeHistory, CancellationToken cancellationToken)
    {
        HashSet<int> pretradeCheckListIds = [.. tradeHistory.TradeChecklists.Select(x => x.PretradeChecklistId)];

        return await context.PretradeChecklists
            .Where(ptc => pretradeCheckListIds.Contains(ptc.Id))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private static string BuildPrompt(string template, TradeSumamryDto summary, List<string> psychologyNotes)
    {
        return template
            .Replace("{{Asset}}", summary.Asset)
            .Replace("{{Position}}", summary.Position)
            .Replace("{{EntryPrice}}", summary.EntryPrice.ToString())
            .Replace("{{TargetTier1}}", summary.TargetTier1.ToString())
            .Replace("{{TargetTier2}}", summary.TargetTier2?.ToString() ?? string.Empty)
            .Replace("{{TargetTier3}}", summary.TargetTier3?.ToString() ?? string.Empty)
            .Replace("{{StopLoss}}", summary.StopLoss.ToString())
            .Replace("{{Notes}}", summary.Notes)
            .Replace("{{ExitPrice}}", summary.ExitPrice?.ToString() ?? string.Empty)
            .Replace("{{Pnl}}", summary.Pnl?.ToString() ?? string.Empty)
            .Replace("{{ConfidenceLevel}}", summary.ConfidenceLevel)
            .Replace("{{TradingZone}}", summary.TradingZone)
            .Replace("{{Date}}", summary.OpenDate.ToShortDateString())
            .Replace("{{ClosedDate}}", summary.ClosedDate.ToShortDateString())
            .Replace("{{TradeTechnicalAnalysisTags}}", string.Join(", ", summary.TradeTechnicalAnalysisTags ?? []))
            .Replace("{{TradeHistoryChecklists}}", string.Join(", ", summary.TradeHistoryChecklists ?? []))
            .Replace("{{EmotionTags}}", string.Join(", ", summary.EmotionTags ?? []))
            .Replace("{{PsychologyNotes}}", string.Join(", ", psychologyNotes ?? []));
    }

    private async Task<GenerateContentResponse> SendGeminiRequest(
        string prompt,
        List<byte[]> imageContents,
        CancellationToken cancellationToken)
    {
        List<Part> imageParts = [.. imageContents.Select(content => new Part
        {
            InlineData = new Blob
            {
                Data = content,
                MimeType = "image/jpeg"
            }
        })];

        List<Part> allPromptParts = [new Part { Text = prompt }];
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

        GenerateContentResponse response = await googleGenAiClient.Models.GenerateContentAsync(
            model: options.Value.Model ?? "gemini-2.5-pro",
            contents: contents,
            config: config,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(response.Text))
        {
            throw new InvalidOperationException("Gemini returned an empty response.");
        }

        return response;
    }

    private static TradeAnalysisResultDto? ParseAiResponse(GenerateContentResponse response)
    {
        try
        {
            return JsonSerializer.Deserialize<TradeAnalysisResultDto>(response.Text!);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse AI response into TradeAnalysisResult. Raw response: {response.Text}", ex);
        }
    }
}
