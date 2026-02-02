namespace Aiursoft.MusicExam.Models.OrderManagementViewModels;

public class UpdateOrderDto
{
    public required List<OrderItem> Items { get; init; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
}
