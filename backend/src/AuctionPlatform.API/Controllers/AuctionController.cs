using AuctionPlatform.Application.Auctions.Commands;
using AuctionPlatform.Application.Auctions.Queries;
using AuctionPlatform.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuctionPlatform.API.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ApplicationDbContext _db;

    public AuctionsController(ISender mediator, ApplicationDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuctions(
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAuctionsQuery(status, categoryId, search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAuction(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetAuctionDetailQuery(id), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Seller,Both")]
    public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionRequest request, CancellationToken ct)
    {
        var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var auctionId = await _mediator.Send(new CreateAuctionCommand(
                request.ItemId, sellerId, request.StartingPrice, request.ReservePrice,
                request.BidIncrement, request.BuyNowPrice, request.StartTime, request.EndTime), ct);

            return CreatedAtAction(nameof(GetAuction), new { id = auctionId }, new { auctionId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{auctionId:guid}/bids")]
    [Authorize]
    public async Task<IActionResult> PlaceBid(Guid auctionId, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        var bidderId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await _mediator.Send(new PlaceBidCommand(auctionId, bidderId, request.Amount), ct);
            await transaction.CommitAsync(ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(ct);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{auctionId:guid}/buy-now")]
    [Authorize]
    public async Task<IActionResult> BuyNow(Guid auctionId, CancellationToken ct)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _mediator.Send(new PlaceBuyNowCommand(auctionId, buyerId), ct);
            await transaction.CommitAsync(ct);
            return Ok(new { message = "Lelang berhasil diselesaikan dengan Buy Now." });
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(ct);
            return BadRequest(new { error = ex.Message });
        }
    }
        /// <summary>Seller membatalkan lelang miliknya sendiri.</summary>
    [HttpDelete("{auctionId:guid}")]
    [Authorize(Roles = "Seller,Both")]
    public async Task<IActionResult> CancelAuction(Guid auctionId, CancellationToken ct)
    {
        var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _mediator.Send(new CancelAuctionCommand(auctionId, sellerId), ct);
            await transaction.CommitAsync(ct);
            return Ok(new { message = "Lelang berhasil dibatalkan." });
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(ct);
            return BadRequest(new { error = ex.Message });
        }
    }

    public record CreateAuctionRequest(
        Guid ItemId,
        decimal StartingPrice,
        decimal? ReservePrice,
        decimal BidIncrement,
        decimal? BuyNowPrice,
        DateTime StartTime,
        DateTime EndTime
    );

    public record PlaceBidRequest(decimal Amount);

    [HttpGet("my-bids")]
    [Authorize]
    public async Task<IActionResult> GetMyBidHistory(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetMyBidHistoryQuery(userId), ct);
        return Ok(result);
    }

    [HttpGet("my-selling")]
    [Authorize]
    public async Task<IActionResult> GetMySellingHistory(CancellationToken ct)
    {
        var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetMySellingHistoryQuery(sellerId), ct);
        return Ok(result);
    }
}