using Pos.Application.Common.Models;

namespace Pos.Application.Features.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(CustomerQuery query, CancellationToken ct);

    Task<CustomerDto> GetAsync(int id, CancellationToken ct);

    /// <summary>Finds a customer by any form of their phone number; <c>null</c> when unknown.</summary>
    Task<CustomerLookupDto?> LookupAsync(string phone, CancellationToken ct);

    Task<CustomerDto> CreateAsync(SaveCustomerRequest request, CancellationToken ct);

    Task<CustomerDto> UpdateAsync(int id, SaveCustomerRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}
