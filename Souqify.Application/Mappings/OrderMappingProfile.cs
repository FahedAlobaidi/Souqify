using AutoMapper;
using Souqify.Application.DTOs.Address;
using Souqify.Application.DTOs.Order;
using Souqify.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Mappings
{
    public class OrderMappingProfile:Profile
    {
        public OrderMappingProfile()
        {
            CreateMap<AddressDto, Address>().ReverseMap();
            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.ProductNameSnapshot))
                .ForMember(d => d.Variant, o => o.MapFrom(s => s.VariantSnapshot))
                .ForMember(d => d.ImageUrl, o => o.MapFrom(s => s.ProductImageSnapshot));
            CreateMap<Order, OrderDto>();
        }
    }
}
