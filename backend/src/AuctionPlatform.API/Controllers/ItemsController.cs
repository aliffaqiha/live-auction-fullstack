using System.Security.Claims;
using AuctionPlatform.Application.Items.Commands;
using AuctionPlatform.Application.Items.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionPlatform.API.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly ISender _mediator;

    public ItemsController(ISender mediator) => _mediator = mediator;

    public record CreateItemRequest(
        Guid CategoryId,
        string Title,
        string Description,
        string Condition,
        List<string> ImageUrls
    );

    [HttpPost]
    [Authorize(Roles = "Seller,Both")]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var itemId = await _mediator.Send(new CreateItemCommand(
                sellerId, request.CategoryId, request.Title, request.Description,
                request.Condition, request.ImageUrls), ct);

            return CreatedAtAction(nameof(GetMyItems), new { }, new { itemId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyItems(CancellationToken ct)
    {
        var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetMyItemsQuery(sellerId), ct);
        return Ok(result);
    }

    [HttpGet("{itemId:guid}/price-history")]
    public async Task<IActionResult> GetPriceHistory(Guid itemId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetItemPriceHistoryQuery(itemId), ct);
        return Ok(result);
    }
    /// <summary>List semua auction untuk item tertentu (milik seller yang login).</summary>
    [HttpGet("{itemId:guid}/auctions")]
    [Authorize]
    public async Task<IActionResult> GetItemAuctions(Guid itemId, CancellationToken ct)
    {
        var auctions = await _mediator.Send(new GetItemAuctionsQuery(itemId), ct);
        return Ok(auctions);
    }
}

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ISender _mediator;

    public CategoriesController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        return Ok(result);
    }
}