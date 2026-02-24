using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Shared.Abstractions;

public abstract class EntityBase<T>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Required]
    public required T Id { get; set; } = default!;

    #region Tracking

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public bool IsDisabled { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }

    #endregion
}