namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for asynchronous equatable operations
/// </summary>
public interface IAsyncEquatable<T>
{
    Task<bool> EqualsAsync(T? other, 
        CancellationToken cancellationToken = default);
}