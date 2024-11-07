namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for asynchronous comparison operations
/// </summary>
public interface IAsyncComparable<T>
{
    Task<bool> CompareToAsync(T? other, 
        CancellationToken cancellationToken = default);
}