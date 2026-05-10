using System;
using Confirmai.Models;

public class OrderMessage
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public OrderModel? Order { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? UserRole { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
}

