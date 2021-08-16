using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevStore.Bff.Compras.Models;
using DevStore.Bff.Compras.Services;
using DevStore.WebAPI.Core.Controllers;

namespace DevStore.Bff.Compras.Controllers
{
    [Authorize]
    public class PedidoController : MainController
    {
        private readonly ICatalogService _catalogService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPedidoService _pedidoService;
        private readonly IClienteService _clienteService;

        public PedidoController(
            ICatalogService catalogService,
            IShoppingCartService shoppingCartService,
            IPedidoService pedidoService,
            IClienteService clienteService)
        {
            _catalogService = catalogService;
            _shoppingCartService = shoppingCartService;
            _pedidoService = pedidoService;
            _clienteService = clienteService;
        }

        [HttpPost]
        [Route("compras/pedido")]
        public async Task<IActionResult> AdicionarPedido(PedidoDTO pedido)
        {
            var carrinho = await _shoppingCartService.GetShoppingCart();
            var produtos = await _catalogService.GetItems(carrinho.Items.Select(p => p.ProductId));
            var endereco = await _clienteService.ObterEndereco();

            if (!await ValidarCarrinhoProdutos(carrinho, produtos)) return CustomResponse();

            PopularDadosPedido(carrinho, endereco, pedido);

            return CustomResponse(await _pedidoService.FinishOrder(pedido));
        }

        [HttpGet("compras/pedido/ultimo")]
        public async Task<IActionResult> UltimoPedido()
        {
            var pedido = await _pedidoService.GetLastOrder();
            if (pedido is null)
            {
                AddErrorToStack("Pedido não encontrado!");
                return CustomResponse();
            }

            return CustomResponse(pedido);
        }

        [HttpGet("compras/pedido/lista-cliente")]
        public async Task<IActionResult> ListaPorCliente()
        {
            var pedidos = await _pedidoService.GetClientsByClientId();

            return pedidos == null ? NotFound() : CustomResponse(pedidos);
        }

        private async Task<bool> ValidarCarrinhoProdutos(ShoppingCartDto shoppingCart, IEnumerable<ProductDto> produtos)
        {
            if (shoppingCart.Items.Count != produtos.Count())
            {
                var itensIndisponiveis = shoppingCart.Items.Select(c => c.ProductId).Except(produtos.Select(p => p.Id)).ToList();

                foreach (var itemId in itensIndisponiveis)
                {
                    var itemCarrinho = shoppingCart.Items.FirstOrDefault(c => c.ProductId == itemId);
                    AddErrorToStack($"O item {itemCarrinho.Name} não está mais disponível no catálogo, o remova do shoppingCart para prosseguir com a compra");
                }

                return false;
            }

            foreach (var itemCarrinho in shoppingCart.Items)
            {
                var produtoCatalogo = produtos.FirstOrDefault(p => p.Id == itemCarrinho.ProductId);

                if (produtoCatalogo.Price != itemCarrinho.Price)
                {
                    var msgErro = $"O produto {itemCarrinho.Name} mudou de valor (de: " +
                                  $"{string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", itemCarrinho.Price)} para: " +
                                  $"{string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", produtoCatalogo.Price)}) desde que foi adicionado ao shoppingCart.";

                    AddErrorToStack(msgErro);

                    var ResponseRemover = await _shoppingCartService.RemoveItem(itemCarrinho.ProductId);
                    if (ResponsePossuiErros(ResponseRemover))
                    {
                        AddErrorToStack($"Não foi possível remover automaticamente o produto {itemCarrinho.Name} do seu shoppingCart, _" +
                                                   "remova e adicione novamente caso ainda deseje comprar este item");
                        return false;
                    }

                    itemCarrinho.Price = produtoCatalogo.Price;
                    var ResponseAdicionar = await _shoppingCartService.AddItem(itemCarrinho);

                    if (ResponsePossuiErros(ResponseAdicionar))
                    {
                        AddErrorToStack($"Não foi possível atualizar automaticamente o produto {itemCarrinho.Name} do seu shoppingCart, _" +
                                                   "adicione novamente caso ainda deseje comprar este item");
                        return false;
                    }

                    CleanErrors();
                    AddErrorToStack(msgErro + " Atualizamos o valor em seu shoppingCart, realize a conferência do pedido e se preferir remova o produto");

                    return false;
                }
            }

            return true;
        }
        
        private void PopularDadosPedido(ShoppingCartDto shoppingCart, EnderecoDTO endereco, PedidoDTO pedido)
        {
            pedido.VoucherCodigo = shoppingCart.Voucher?.Code;
            pedido.VoucherUtilizado = shoppingCart.HasVoucher;
            pedido.ValorTotal = shoppingCart.Total;
            pedido.Desconto = shoppingCart.Discount;
            pedido.PedidoItems = shoppingCart.Items;

            pedido.Endereco = endereco;
        }
    }
}
