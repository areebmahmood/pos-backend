using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Customers;

public static class CustomerMappings
{
    public static readonly Expression<Func<Customer, CustomerDto>> Projection = c => new CustomerDto(
        c.Id,
        c.Name,
        c.Phone,
        c.OptedOut,
        c.Sales.Count(),
        c.Sales.Sum(s => s.Total - s.ReturnedAmount),
        c.Sales.Max(s => (DateTime?)s.CreatedAt),
        c.CreatedAt);
}
