using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
using Souqify.Application.Mappings;
using Souqify.Application.Services;
using Souqify.Extensions;
using Souqify.Infrastructure;
using Souqify.Infrastructure.Queries;
using Souqify.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(ProductMappingProfile).Assembly);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDatabase(connectionString);

builder.Services.AddScoped<IAdminProductQueries, AdminProductQueries>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductQueries, ProductQueries>();
builder.Services.AddScoped<ICategoryQueries, CategoryQueries>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
