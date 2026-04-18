using FluentAssertions;
using Souqify.Application.DTOs.Product;
using Souqify.Domain;
using SouqifyTests.Helper;
using System.Net.Http.Json;


namespace SouqifyTests.ProductController
{
    public class ProductControllerTest:IClassFixture<SouqifyApiFactory>
    {
        private readonly SouqifyApiFactory _souqifyApiFactory;
        private readonly HttpClient _httpClient;

        public ProductControllerTest(SouqifyApiFactory souqifyApiFactory)
        {
            _souqifyApiFactory = souqifyApiFactory;
            _httpClient = _souqifyApiFactory.CreateClient();
        }

        [Fact]
        public async Task GetProductById_ReturnOk()
        {
            var product = await SeedHelper.SeedProductAsync(_souqifyApiFactory.Services);

            var response = await _httpClient.GetAsync($"/api/products/{product.Id}");

            var returenedProduct = await response.Content.ReadFromJsonAsync<ProductDetailDto>();

            returenedProduct.Should().NotBeNull();
            returenedProduct.Id.Should().Be(product.Id);
            returenedProduct.Variants.Should().HaveCount(1);
            returenedProduct.ProductImages.Should().HaveCount(1);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetProductById_NotExist_ReturnNotFound()
        {
            var response = await _httpClient.GetAsync($"/api/products/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAllProduct_ReturnOk()
        {
            var products = new List<Product>();

            for(int i = 0; i < 5; i++)
            {
                products.Add(await SeedHelper.SeedProductAsync(_souqifyApiFactory.Services));
            }

            var response = await _httpClient.GetAsync("/api/products");
            var requestedProducts = await response.Content.ReadFromJsonAsync<List<ProductListDto>>();

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            requestedProducts.Should().NotBeNull();
            requestedProducts!.Count.Should().BeGreaterThan(0);
        }

    }
}
