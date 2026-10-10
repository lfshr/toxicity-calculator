namespace ChocolateToxicity.ProductSearch;

public interface IProductSearch
{
    /// <summary>Finds products matching free text such as a brand and product name.</summary>
    /// <exception cref="ProductSearchException">The search service could not be reached or refused the request.</exception>
    Task<IReadOnlyList<ChocolateProduct>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up one product's full record, which can hold cocoa data the search results lack.
    /// Returns null when the product can't be fetched.
    /// </summary>
    Task<ChocolateProduct?> GetProductAsync(string barcode, CancellationToken cancellationToken = default);
}

public sealed class ProductSearchException(string message, Exception? innerException = null) : Exception(message, innerException);
