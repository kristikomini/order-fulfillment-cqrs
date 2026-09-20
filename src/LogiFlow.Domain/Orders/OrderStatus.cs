namespace LogiFlow.Domain.Orders;

public enum OrderStatus
{
    /// <summary>Created, stock not yet reserved.</summary>
    Pending = 0,
    /// <summary>All lines reserved against inventory.</summary>
    Confirmed = 1,
    /// <summary>Could not be satisfied (e.g. insufficient stock).</summary>
    Rejected = 2,
    /// <summary>Reserved stock has shipped.</summary>
    Fulfilled = 3
}
