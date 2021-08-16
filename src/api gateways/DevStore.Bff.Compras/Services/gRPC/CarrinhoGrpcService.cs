using DevStore.Bff.Compras.Models;
using DevStore.ShoppingCart.API.Services.gRPC;
using System;
using System.Threading.Tasks;

namespace DevStore.Bff.Compras.Services.gRPC
{
    public interface ICarrinhoGrpcService
    {
        Task<ShoppingCartDto> GetShoppingCart();
    }

    public class CarrinhoGrpcService : ICarrinhoGrpcService
    {
        private readonly ShoppingCartOrders.ShoppingCartOrdersClient _carrinhoComprasClient;

        public CarrinhoGrpcService(ShoppingCartOrders.ShoppingCartOrdersClient carrinhoComprasClient)
        {
            _carrinhoComprasClient = carrinhoComprasClient;
        }

        public async Task<ShoppingCartDto> GetShoppingCart()
        {
            var Response = await _carrinhoComprasClient.GetShoppingCartAsync(new GetShoppingCartRequest());
            return MapShoppingCartProtoResponseDto(Response);
        }

        private static ShoppingCartDto MapShoppingCartProtoResponseDto(ShoppingCartClientClientResponse carrinhoResponse)
        {
            var cartDto = new ShoppingCartDto
            {
                Total = (decimal)carrinhoResponse.Total,
                Discount = (decimal)carrinhoResponse.Discount,
                HasVoucher = carrinhoResponse.Hasvoucher
            };

            if (carrinhoResponse.Voucher != null)
            {
                cartDto.Voucher = new VoucherDTO
                {
                    Code = carrinhoResponse.Voucher.Code,
                    Percentage = (decimal?)carrinhoResponse.Voucher.Percentage,
                    Discount = (decimal?)carrinhoResponse.Voucher.Discount,
                    DiscountType = carrinhoResponse.Voucher.Discounttype
                };
            }

            foreach (var item in carrinhoResponse.Items)
            {
                cartDto.Items.Add(new ShoppingCartItemDto
                {
                    Name = item.Name,
                    Image = item.Image,
                    ProductId = Guid.Parse(item.Productid),
                    Quantity = item.Quantity,
                    Price = (decimal)item.Price
                });
            }

            return cartDto;
        }
    }
}