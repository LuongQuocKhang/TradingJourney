using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Text;
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
    HttpClient httpClient,
    IImageHelper imageHelper,
    IOptions<OpenRouterOptions> options,
    IHttpContextAccessor httpContextAccessor) : IGoogleGenAIService
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

        string responseText = await SendOpenRouterRequest(finalPrompt, imageContents, cancellationToken);

        return ParseAiResponse(responseText);
    }

    private async Task<TradeHistory> LoadTradeHistory(int tradeHistoryId, CancellationToken cancellationToken)
    {
        return await context.TradeHistories
            .AsNoTracking()
            .Include(th => th.TradeScreenShots)
            .Include(th => th.TradeEmotionTags)
            .Include(th => th.TradeChecklists)
                .ThenInclude(th => th.PretradeChecklist)
            .Include(th => th.TradeTechnicalAnalysisTags)
                .ThenInclude(th => th.TechnicalAnalysis)
            .AsSplitQuery()
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

    private async Task<string> SendOpenRouterRequest(
        string prompt,
        List<byte[]> imageContents,
        CancellationToken cancellationToken)
    {
        List<object> allPromptParts =
        [
            new { type = "text", text = prompt }
        ];

        foreach (byte[] content in imageContents)
        {
            allPromptParts.Add(new
            {
                type = "image_url",
                image_url = new
                {
                    url = $"data:image/jpeg;base64,{Convert.ToBase64String(content)}"
                }
            });
        }

        var requestBody = new
        {
            model = options.Value.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = allPromptParts
                }
            }
        };

        using HttpRequestMessage request = new(HttpMethod.Post, $"{options.Value.BaseUrl}/chat/completions");
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);

        HttpContext? httpContext = httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            request.Headers.Add("HTTP-Referer", $"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
        }
        else
        {
            request.Headers.Add("HTTP-Referer", "http://localhost:3000");
        }
        request.Headers.Add("X-Title", "TradingJournal");

        string jsonBody = JsonSerializer.Serialize(requestBody);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"OpenRouter API failed with status {response.StatusCode}: {errorContent}");
        }

        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        using JsonDocument doc = JsonDocument.Parse(responseContent);
        JsonElement root = doc.RootElement;
        
        if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
        {
            JsonElement message = choices[0].GetProperty("message");
            if (message.TryGetProperty("content", out JsonElement textContent))
            {
                return textContent.GetString() ?? string.Empty;
            }
        }

        throw new InvalidOperationException("OpenRouter returned an empty or invalid response.");
    }

    private static TradeAnalysisResultDto? ParseAiResponse(string responseText)
    {
        try
        {
            string cleanText = responseText.Trim();
            if (cleanText.StartsWith("```json"))
            {
                cleanText = cleanText.Substring(7);
            }
            if (cleanText.StartsWith("```"))
            {
                cleanText = cleanText.Substring(3);
            }
            if (cleanText.EndsWith("```"))
            {
                cleanText = cleanText.Substring(0, cleanText.Length - 3);
            }

            JsonSerializerOptions serializeOptions = new()
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<TradeAnalysisResultDto>(cleanText.Trim(), serializeOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse AI response into TradeAnalysisResult. Raw response: {responseText}", ex);
        }
    }
}
