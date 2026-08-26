namespace backend.Dtos;

public class SupplierPurchaseSuggestionDto
{
    public int supplierId { get; set; }
    public string supplierName { get; set; } = string.Empty;
    public List<PurchaseSuggestionItemDto> items { get; set; } = new();
}

public class PurchaseSuggestionItemDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public int currentStock { get; set; }
    public int stockWarning { get; set; }
    public int suggestedQuantity { get; set; }
}
