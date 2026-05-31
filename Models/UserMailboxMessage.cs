using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models;

public class UserMailboxMessage
{
    public int Id { get; set; }

    public string? SenderUserId { get; set; }
    public ApplicationUser? SenderUser { get; set; }

    /// <summary>Nome do remetente capturado no momento do envio (preservado mesmo após exclusão da conta).</summary>
    [StringLength(100)]
    public string? SenderDisplayName { get; set; }

    [Required]
    public string RecipientUserId { get; set; } = string.Empty;
    public ApplicationUser? RecipientUser { get; set; }

    /// <summary>Nome do destinatário capturado no momento do envio (preservado mesmo após exclusão da conta).</summary>
    [StringLength(100)]
    public string? RecipientDisplayName { get; set; }

    [StringLength(180)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public int? RelatedServerId { get; set; }

    [StringLength(120)]
    public string? RelatedItemKey { get; set; }

    [StringLength(300)]
    public string? AttachmentPath { get; set; }

    [StringLength(260)]
    public string? AttachmentName { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public bool IsArchivedBySender { get; set; }
    public DateTime? ArchivedBySenderAt { get; set; }

    public bool IsArchivedByRecipient { get; set; }
    public DateTime? ArchivedByRecipientAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}