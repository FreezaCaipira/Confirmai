using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models;

/// <summary>
/// Lote de repasse da taxa da plataforma enviado pelo organizador do grupo.
/// Espelha o fluxo do comprovante de pagamento do jogador (nivel 1),
/// mas no nivel 2: organizador paga -> envia comprovante -> admin do sistema confirma.
/// </summary>
public class PlatformFeeSettlement
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    /// <summary>Valor declarado pelo organizador no repasse.</summary>
    public decimal Amount { get; set; }

    /// <summary>UserId do organizador que enviou o repasse.</summary>
    [StringLength(450)]
    public string SubmittedByUserId { get; set; } = string.Empty;
    public ApplicationUser SubmittedByUser { get; set; } = null!;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Bytes da imagem do comprovante (mesmo padrao de EventConfirmation.PixProofImageData).</summary>
    public byte[]? ProofImageData { get; set; }

    /// <summary>MIME type do comprovante.</summary>
    [StringLength(100)]
    public string? ProofContentType { get; set; }

    public PlatformFeeSettlementStatus Status { get; set; } = PlatformFeeSettlementStatus.EmAnalise;

    /// <summary>UserId do admin do sistema que revisou.</summary>
    [StringLength(450)]
    public string? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Motivo da rejeicao ou observacao do admin.</summary>
    [StringLength(500)]
    public string? ReviewNote { get; set; }

    /// <summary>
    /// Comma-separated list of EventIds selected by the organizer when submitting
    /// this settlement. When the settlement is approved (Pago), these matches are
    /// considered covered by this settlement.
    /// </summary>
    [StringLength(1000)]
    public string? SelectedEventIds { get; set; }
}

public enum PlatformFeeSettlementStatus
{
    EmAnalise = 0,
    Pago = 1,
    Rejeitado = 2
}
