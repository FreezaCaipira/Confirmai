using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

public sealed record AdminOrderReleaseResult(bool Succeeded, string Reason);

public class AdminOrderReleaseService
{
    private readonly AppDbContext _db;
    private readonly LogService _logService;
    private readonly OperationFeeCalculatorService _operationFeeCalculator;

    public AdminOrderReleaseService(
        AppDbContext db,
        LogService logService,
        OperationFeeCalculatorService operationFeeCalculator)
    {
        _db = db;
        _logService = logService;
        _operationFeeCalculator = operationFeeCalculator;
    }

    public async Task<AdminOrderReleaseResult> ReleaseFundsAsync(int orderId, string? adminUserId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            await _logService.LogAsync(
                $"Tentativa de repasse para pedido inexistente (OrderId: {orderId}).",
                source: AdminAuditSources.OrdersReview,
                level: AdminAuditLevels.Refused,
                userId: adminUserId);

            return new AdminOrderReleaseResult(false, "OrderNotFound");
        }

        if (order.Status != PaymentStatus.AguardandoRevisaoAdm)
        {
            await _logService.LogAsync(
                $"Tentativa de repasse recusada por status invalido (OrderId: {orderId}, StatusAtual: {order.Status}).",
                source: AdminAuditSources.OrdersReview,
                level: AdminAuditLevels.Refused,
                userId: adminUserId);

            return new AdminOrderReleaseResult(false, "InvalidStatus");
        }

        if (order.BuyerId == null || order.SellerId == null)
        {
            await _logService.LogAsync(
                $"Tentativa de repasse recusada: participante removido (OrderId: {orderId}).",
                source: AdminAuditSources.OrdersReview,
                level: AdminAuditLevels.Refused,
                userId: adminUserId);

            return new AdminOrderReleaseResult(false, "ParticipantDeleted");
        }

        var previousStatus = order.Status;
        var breakdown = await _operationFeeCalculator.CalculateBreakdownAsync(order.Amount);

        order.Status = PaymentStatus.Finalizado;
        order.FundsReleased = true;
        await _db.SaveChangesAsync();

        await MailboxConversationArchiveService.ArchiveOrderConversationForAllParticipantsAsync(_db, orderId);

        await _logService.AuditAsync(
            AuditEvents.OrderReleased,
            AuditEntities.Order,
            orderId.ToString(),
            $"Repasse de fundos confirmado (OrderId: {orderId}, StatusAnterior: {previousStatus}, StatusNovo: {order.Status}, GrossBTC: {order.Amount:0.########}, FeePercent: {breakdown.Percent:0.##}, FeeBTC: {breakdown.FeeAmount:0.########}, NetBTC: {breakdown.NetAmount:0.########}).",
            actorUserId: adminUserId,
            source: AdminAuditSources.OrdersReview,
            level: AdminAuditLevels.Success,
            metadata: new
            {
                OrderId = orderId,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = order.Status.ToString(),
                GrossBtc = order.Amount,
                FeePercent = breakdown.Percent,
                FeeBtc = breakdown.FeeAmount,
                NetBtc = breakdown.NetAmount,
                order.BuyerId,
                order.SellerId
            });

        return new AdminOrderReleaseResult(true, "Released");
    }
}


