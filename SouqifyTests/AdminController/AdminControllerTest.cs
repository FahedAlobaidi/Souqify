using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Souqify;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Domain;
using SouqifyTests.Helper;
using System.Net.Http.Json;


namespace SouqifyTests.AdminController
{
    public class AdminControllerTest:IClassFixture<SouqifyApiFactory>
    {
        private readonly SouqifyApiFactory _adminApiFactory;
        private readonly HttpClient _httpClient;
       

        public AdminControllerTest(SouqifyApiFactory adminApiFactory)
        {
            _adminApiFactory = adminApiFactory;
            _httpClient = _adminApiFactory.CreateClient();
        }

        //[Fact]
        //public async Task GetProducts_ReturnsOk()
        //{
            
        //    var response = await _httpClient.GetAsync("/api/products");

        //    response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        //}

        [Fact]
        public async Task CreateProduct_ReturnCreated()
        {
            var faker = new Faker();

            var category = await SeedHelper.SeedCategoryAsync(_adminApiFactory.Services);

            var productDto = new CreateProductDto
            {
                Brand = faker.Company.CompanyName(),
                Name=faker.Commerce.ProductName(),
                Variants=new List<CreateVariantDto>
                {
                    new CreateVariantDto
                    {
                        SKU=faker.Random.AlphaNumeric(10).ToUpper(),
                        Color=faker.Commerce.Color(),
                        LowStockThreshold=5,
                        PriceAdjustment=faker.Random.Int(1,10),
                        Size=faker.PickRandom("S","M","L","XL"),
                        StockQuantity=faker.Random.Int(1,100)
                    }
                },
                ProductImages = new List<CreateImageDto>
                {
                    new CreateImageDto
                    {
                        ImageUrl=faker.Image.PicsumUrl(),
                        DisplayOrder=1,
                        IsMain=true
                    }
                },
                BasePrice=decimal.Parse(faker.Commerce.Price(1,100)),
                CategoryId=category.Id,
                Description=faker.Commerce.ProductDescription(),
                IsFeatured=true
            };

            var response = await _httpClient.PostAsJsonAsync("/api/admin/products", productDto);

            var createdProduct = await response.Content.ReadFromJsonAsync<AdminProductDto>();

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            createdProduct.Should().NotBeNull();
            createdProduct.Name.Should().Be(productDto.Name);
            createdProduct.Brand.Should().Be(productDto.Brand);
            createdProduct.BasePrice.Should().Be(productDto.BasePrice);
            createdProduct.AllVariants.Should().HaveCount(1);
            createdProduct.Images.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateProduct_DuplicatedSKU_ReturnBadRequest()
        {
            var category = await SeedHelper.SeedCategoryAsync(_adminApiFactory.Services);

            var faker = new Faker();

            var duplicatedSKU = "DUPLICATE-SKU-001";

            var productDto = new CreateProductDto
            {
                Brand = faker.Company.CompanyName(),
                Name = faker.Commerce.ProductName(),
                Variants = new List<CreateVariantDto>
                {
                    new CreateVariantDto
                    {
                        SKU=duplicatedSKU,
                        Color=faker.Commerce.Color(),
                        LowStockThreshold=5,
                        PriceAdjustment=faker.Random.Int(1,10),
                        Size=faker.PickRandom("S","M","L","XL"),
                        StockQuantity=faker.Random.Int(1,100)
                    }
                },
                ProductImages = new List<CreateImageDto>
                {
                    new CreateImageDto
                    {
                        ImageUrl=faker.Image.PicsumUrl(),
                        DisplayOrder=1,
                        IsMain=true
                    }
                },
                BasePrice = decimal.Parse(faker.Commerce.Price(1, 100)),
                CategoryId = category.Id,
                Description = faker.Commerce.ProductDescription(),
                IsFeatured = true
            };

            await _httpClient.PostAsJsonAsync("/api/admin/products", productDto);

            var productDto2 = new CreateProductDto
            {
                Brand = faker.Company.CompanyName(),
                Name = faker.Commerce.ProductName(),
                Variants = new List<CreateVariantDto>
                {
                    new CreateVariantDto
                    {
                        SKU=duplicatedSKU,
                        Color=faker.Commerce.Color(),
                        LowStockThreshold=5,
                        PriceAdjustment=faker.Random.Int(1,10),
                        Size=faker.PickRandom("S","M","L","XL"),
                        StockQuantity=faker.Random.Int(1,100)
                    }
                },
                ProductImages = new List<CreateImageDto>
                {
                    new CreateImageDto
                    {
                        ImageUrl=faker.Image.PicsumUrl(),
                        DisplayOrder=1,
                        IsMain=true
                    }
                },
                BasePrice = decimal.Parse(faker.Commerce.Price(1, 100)),
                CategoryId = category.Id,
                Description = faker.Commerce.ProductDescription(),
                IsFeatured = true
            };

            var response = await _httpClient.PostAsJsonAsync("/api/admin/products", productDto2);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);

        }

        [Fact]
        public async Task CreateProduct_InvalidCategoryId_ReturnsInternalServerError()
        {
            var faker = new Faker();

            var productDto = new CreateProductDto
            {
                Brand = faker.Company.CompanyName(),
                Name = faker.Commerce.ProductName(),
                Variants = new List<CreateVariantDto>
                {
                    new CreateVariantDto
                    {
                        SKU=faker.Random.AlphaNumeric(10).ToUpper(),
                        Color=faker.Commerce.Color(),
                        LowStockThreshold=5,
                        PriceAdjustment=faker.Random.Int(1,10),
                        Size=faker.PickRandom("S","M","L","XL"),
                        StockQuantity=faker.Random.Int(1,100)
                    }
                },
                ProductImages = new List<CreateImageDto>
                {
                    new CreateImageDto
                    {
                        ImageUrl=faker.Image.PicsumUrl(),
                        DisplayOrder=1,
                        IsMain=true
                    }
                },
                BasePrice = decimal.Parse(faker.Commerce.Price(1, 100)),
                CategoryId = Guid.NewGuid(),
                Description = faker.Commerce.ProductDescription(),
                IsFeatured = true
            };

            var response = await _httpClient.PostAsJsonAsync("/api/admin/products", productDto);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task CreatedProduct_NegativeFinalPrice_ReturnInternalServerError()
        {
            var faker = new Faker();

            var category = await SeedHelper.SeedCategoryAsync(_adminApiFactory.Services);


            var productDto = new CreateProductDto
            {
                Brand = faker.Company.CompanyName(),
                Name = faker.Commerce.ProductName(),
                Variants = new List<CreateVariantDto>
                {
                    new CreateVariantDto
                    {
                        SKU=faker.Random.AlphaNumeric(10).ToUpper(),
                        Color=faker.Commerce.Color(),
                        LowStockThreshold=5,
                        PriceAdjustment=-10,
                        Size=faker.PickRandom("S","M","L","XL"),
                        StockQuantity=faker.Random.Int(1,100)
                    }
                },
                ProductImages = new List<CreateImageDto>
                {
                    new CreateImageDto
                    {
                        ImageUrl=faker.Image.PicsumUrl(),
                        DisplayOrder=1,
                        IsMain=true
                    }
                },
                BasePrice = 5,
                CategoryId = category.Id,
                Description = faker.Commerce.ProductDescription(),
                IsFeatured = true
            };

            var response = await _httpClient.PostAsJsonAsync("/api/admin/products", productDto);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task UpdateProduct_ReturnOk()
        {
            var faker = new Faker();

            var product = await SeedHelper.SeedProductAsync(_adminApiFactory.Services);
            var category = await SeedHelper.SeedCategoryAsync(_adminApiFactory.Services);

            var updateProduct = new UpdateProductDto
            {
                BasePrice = decimal.Parse(faker.Commerce.Price(1, 100)),
                Brand = faker.Company.CompanyName(),
                CategoryId = category.Id,
                Description = faker.Commerce.ProductDescription(),
            };

            //act
            var response = await _httpClient.PutAsJsonAsync($"/api/admin/products/{product.Id}", updateProduct);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task ToggleProductStatus_ReturnOk()
        {
            var product = await SeedHelper.SeedProductAsync(_adminApiFactory.Services);

            var response = await _httpClient.PatchAsync($"/api/admin/products/{product.Id}/status",null);

            var returnedValue = await response.Content.ReadFromJsonAsync<bool>();

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            returnedValue.Should().Be(false);
        }
    }
}
