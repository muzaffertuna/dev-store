using DevStore.Core.Communication;
using DevStore.WebApp.MVC.Extensions;
using DevStore.WebApp.MVC.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DevStore.WebApp.MVC.Services
{
    public interface IComprasBffService
    {
        // Carrinho
        Task<ShoppingCartViewModel> GetShoppingCart();
        Task<int> ObterQuantidadeCarrinho();
        Task<ResponseResult> AddShoppingCartItem(ShoppingCartItemViewModel carrinho);
        Task<ResponseResult> AtualizarItemCarrinho(Guid produtoId, ShoppingCartItemViewModel carrinho);
        Task<ResponseResult> RemoverItemCarrinho(Guid produtoId);
        Task<ResponseResult> AplicarVoucherCarrinho(string voucher);

        // Pedido
        Task<ResponseResult> FinalizarPedido(PedidoTransacaoViewModel pedidoTransacao);
        Task<PedidoViewModel> ObterUltimoPedido();
        Task<IEnumerable<PedidoViewModel>> ObterListaPorClienteId();
        PedidoTransacaoViewModel MapearParaPedido(ShoppingCartViewModel shoppingCart, EnderecoViewModel endereco);
    }

    public class ComprasBffService : Service, IComprasBffService
    {
        private readonly HttpClient _httpClient;

        public ComprasBffService(HttpClient httpClient, IOptions<AppSettings> settings)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(settings.Value.ComprasBffUrl);
        }

        #region Carrinho

        public async Task<ShoppingCartViewModel> GetShoppingCart()
        {
            var response = await _httpClient.GetAsync("/orders/shopping-cart/");

            ManageResponseErrors(response);

            return await DeserializeResponse<ShoppingCartViewModel>(response);
        }
        public async Task<int> ObterQuantidadeCarrinho()
        {
            var Response = await _httpClient.GetAsync("/orders/shopping-cart/quantity/");

            ManageResponseErrors(Response);

            return await DeserializeResponse<int>(Response);
        }
        public async Task<ResponseResult> AddShoppingCartItem(ShoppingCartItemViewModel carrinho)
        {
            var itemContent = GetContent(carrinho);

            var Response = await _httpClient.PostAsync("/orders/shopping-cart/items/", itemContent);

            if (!ManageResponseErrors(Response)) return await DeserializeResponse<ResponseResult>(Response);

            return RetornoOk();
        }
        public async Task<ResponseResult> AtualizarItemCarrinho(Guid produtoId, ShoppingCartItemViewModel shoppingCartItem)
        {
            var itemContent = GetContent(shoppingCartItem);

            var Response = await _httpClient.PutAsync($"/orders/shopping-cart/items/{produtoId}", itemContent);

            if (!ManageResponseErrors(Response)) return await DeserializeResponse<ResponseResult>(Response);

            return RetornoOk();
        }
        public async Task<ResponseResult> RemoverItemCarrinho(Guid produtoId)
        {
            var Response = await _httpClient.DeleteAsync($"/orders/shopping-cart/items/{produtoId}");

            if (!ManageResponseErrors(Response)) return await DeserializeResponse<ResponseResult>(Response);

            return RetornoOk();
        }
        public async Task<ResponseResult> AplicarVoucherCarrinho(string voucher)
        {
            var itemContent = GetContent(voucher);

            var Response = await _httpClient.PostAsync("/orders/shopping-cart/aplicar-voucher/", itemContent);

            if (!ManageResponseErrors(Response)) return await DeserializeResponse<ResponseResult>(Response);

            return RetornoOk();
        }

        #endregion

        #region Pedido

        public async Task<ResponseResult> FinalizarPedido(PedidoTransacaoViewModel pedidoTransacao)
        {
            var pedidoContent = GetContent(pedidoTransacao);

            var Response = await _httpClient.PostAsync("/compras/pedido/", pedidoContent);

            if (!ManageResponseErrors(Response)) return await DeserializeResponse<ResponseResult>(Response);

            return RetornoOk();
        }

        public async Task<PedidoViewModel> ObterUltimoPedido()
        {
            var Response = await _httpClient.GetAsync("/compras/pedido/ultimo/");

            ManageResponseErrors(Response);

            return await DeserializeResponse<PedidoViewModel>(Response);
        }

        public async Task<IEnumerable<PedidoViewModel>> ObterListaPorClienteId()
        {
            var Response = await _httpClient.GetAsync("/compras/pedido/lista-cliente/");

            ManageResponseErrors(Response);

            return await DeserializeResponse<IEnumerable<PedidoViewModel>>(Response);
        }

        public PedidoTransacaoViewModel MapearParaPedido(ShoppingCartViewModel shoppingCart, EnderecoViewModel endereco)
        {
            var pedido = new PedidoTransacaoViewModel
            {
                ValorTotal = shoppingCart.Total,
                Itens = shoppingCart.Items,
                Desconto = shoppingCart.Discount,
                VoucherUtilizado = shoppingCart.HasVoucher,
                VoucherCodigo = shoppingCart.Voucher?.Codigo
            };

            if (endereco != null)
            {
                pedido.Endereco = new EnderecoViewModel
                {
                    Logradouro = endereco.Logradouro,
                    Numero = endereco.Numero,
                    Bairro = endereco.Bairro,
                    Cep = endereco.Cep,
                    Complemento = endereco.Complemento,
                    Cidade = endereco.Cidade,
                    Estado = endereco.Estado
                };
            }

            return pedido;
        }

        #endregion
    }
}