using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Domain.Common;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Application.Features.Customers;

public sealed class CustomerService(
    ICustomerRepository customers,
    IStoreSettingsRepository settings,
    IUnitOfWork unitOfWork,
    IValidator<SaveCustomerRequest> validator) : ICustomerService
{
    public Task<PagedResult<CustomerDto>> ListAsync(CustomerQuery query, CancellationToken ct) =>
        customers.ListAsync(new CustomerFilter(query, query.Search, query.OptedOut), CustomerMappings.Projection, ct);

    public async Task<CustomerDto> GetAsync(int id, CancellationToken ct) =>
        await customers.FindAsync(id, CustomerMappings.Projection, ct) ?? throw NotFoundException.For("Customer", id);

    public async Task<CustomerLookupDto?> LookupAsync(string phone, CancellationToken ct)
    {
        var normalized = PhoneNumber.Normalize(phone, (await settings.GetAsync(ct)).DefaultCountryCode);
        if (normalized is null)
            return null;

        var customer = await customers.GetByPhoneAsync(normalized, ct);
        return customer is null ? null : new CustomerLookupDto(customer.Id, customer.Name, customer.Phone);
    }

    public async Task<CustomerDto> CreateAsync(SaveCustomerRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var phone = await NormalizeUniqueAsync(request.Phone, null, ct);

        var customer = new Customer(request.Name, phone);
        customer.Update(request.Name, phone, request.OptedOut);
        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(customer.Id, ct);
    }

    public async Task<CustomerDto> UpdateAsync(int id, SaveCustomerRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var customer = await customers.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Customer", id);
        var phone = await NormalizeUniqueAsync(request.Phone, id, ct);

        customer.Update(request.Name, phone, request.OptedOut);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Customer", id);
        customers.Remove(customer);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<string> NormalizeUniqueAsync(string phone, int? excludeId, CancellationToken ct)
    {
        var normalized = PhoneNumber.Normalize(phone, (await settings.GetAsync(ct)).DefaultCountryCode)
            ?? throw new DomainException("Enter a valid phone number, e.g. 03001234567 or +923001234567.");

        if (await customers.PhoneExistsAsync(normalized, excludeId, ct))
            throw new ConflictException($"A customer with phone {normalized} already exists.");
        return normalized;
    }
}
