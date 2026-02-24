using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Modules.Trades.Common.Enum;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "PretradeChecklists", Schema = "Trades")]
public sealed class PretradeChecklist : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;

    public RetradeCheckListType CheckListType { get; set; }
}
