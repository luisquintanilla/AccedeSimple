using System;

namespace AccedeSimple.Domain
{
    public record ReceiptData
    {
        public string Id { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public ExpenseCategory Category { get; init; }
        public DateTime Date { get; init; }
        public string ImageUrl { get; init; } = string.Empty;

        public ReceiptData WithCategory(ExpenseCategory newCategory) =>
            this with { Category = newCategory };

        public ReceiptData WithAmount(decimal newAmount) =>
            this with { Amount = newAmount };
    }
}