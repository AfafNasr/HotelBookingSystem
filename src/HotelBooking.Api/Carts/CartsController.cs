using HotelBooking.Api.Common;
using HotelBooking.Application.Carts.AddToCart;
using HotelBooking.Application.Carts.GetCart;
using HotelBooking.Application.Carts.RemoveCartItem;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Carts;

[ApiController]
[Route("api/cart")]
public sealed class CartsController : ControllerBase
{
    private readonly AddToCartCommandHandler _addToCartHandler;
    private readonly RemoveCartItemCommandHandler _removeCartItemHandler;
    private readonly GetCartQueryHandler _getCartHandler;

    public CartsController(
    AddToCartCommandHandler addToCartHandler,
    RemoveCartItemCommandHandler removeCartItemHandler,
    GetCartQueryHandler getCartHandler)
    {
        _addToCartHandler = addToCartHandler;
        _removeCartItemHandler = removeCartItemHandler;
        _getCartHandler = getCartHandler;
    }

    [HttpPost("items")]
    [Authorize(Policy = UserPermissions.CartManage)]
    public async Task<IActionResult> AddItem(
        AddToCartRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new AddToCartCommand(
                request.RoomId);

        var result =
            await _addToCartHandler.HandleAsync(
                command,
                cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response =
            new AddToCartResponse(
                result.CartId!.Value);

        return Ok(response);
    }

    [HttpDelete("items/{roomId:int}")]
    [Authorize(Policy = UserPermissions.CartManage)]
    public async Task<IActionResult> RemoveItem(
    int roomId,
    CancellationToken cancellationToken)
    {
        var result =
            await _removeCartItemHandler.HandleAsync(
                new RemoveCartItemCommand(roomId),
                cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = UserPermissions.CartManage)]
    public async Task<IActionResult> Get(
     CancellationToken cancellationToken)
    {
        var result =
            await _getCartHandler.HandleAsync(
                cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        if (result.Cart is null)
        {
            return Ok(
                new GetCartResponse(
                    null,
                    null,
                    null,
                    []));
        }

        var cart =
            result.Cart;

        var response =
            new GetCartResponse(
                cart.CartId,
                cart.HotelId,
                cart.HotelName,
                cart.Items
                    .Select(item =>
                        new CartItemResponse(
                            item.RoomId,
                            item.RoomNumber,
                            item.RoomType,
                            item.Description,
                            item.AdultsCapacity,
                            item.ChildrenCapacity,
                            item.PricePerNight,
                            item.IsRoomActive,
                            item.AddedAt))
                    .ToArray());

        return Ok(response);
    }
}