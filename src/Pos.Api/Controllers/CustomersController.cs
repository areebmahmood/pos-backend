using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Customers;
using Pos.Application.Features.Messaging;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/customers")]
public sealed class CustomersController(ICustomerService customers, IMessagingService messaging) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public Task<PagedResult<CustomerDto>> List([FromQuery] CustomerQuery query, CancellationToken ct) =>
        customers.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<CustomerDto> Get(int id, CancellationToken ct) => customers.GetAsync(id, ct);

    /// <summary>Used while billing to fill in a returning customer's name.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType<CustomerLookupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Lookup([FromQuery] string phone, CancellationToken ct) =>
        await customers.LookupAsync(phone, ct) is { } customer ? Ok(customer) : NoContent();

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveCustomerRequest request, CancellationToken ct)
    {
        var customer = await customers.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<CustomerDto> Update(int id, SaveCustomerRequest request, CancellationToken ct) =>
        customers.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await customers.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("promotions")]
    [Authorize(Roles = Roles.Admin)]
    public Task<PagedResult<PromotionDto>> ListPromotions([FromQuery] PageRequest page, CancellationToken ct) =>
        messaging.ListPromotionsAsync(page, ct);

    [HttpPost("promotions")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<PromotionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> SendPromotion(SendPromotionRequest request, CancellationToken ct)
    {
        var promotion = await messaging.SendPromotionAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, promotion);
    }

    [HttpGet("messages")]
    [Authorize(Roles = Roles.Admin)]
    public Task<PagedResult<OutgoingMessageDto>> ListMessages([FromQuery] MessageQuery query, CancellationToken ct) =>
        messaging.ListMessagesAsync(query, ct);
}
