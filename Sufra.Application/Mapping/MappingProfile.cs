using AutoMapper;
using Sufra.Domain.Entities;
using Sufra.Application.DTOs.Students;
using Sufra.Application.DTOs.Subscriptions;
using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.DTOs.Batches;
using Sufra.Application.DTOs.Couriers;
using Sufra.Application.DTOs.Deliveries;
using Sufra.Application.DTOs.Zones;

namespace Sufra.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 👤 الطلاب
            CreateMap<Student, StudentDto>().ReverseMap();
            CreateMap<CreateStudentDto, Student>();
            CreateMap<UpdateStudentDto, Student>();

            // 💳 الاشتراكات
            CreateMap<Subscription, SubscriptionDto>().ReverseMap();
            CreateMap<CreateSubscriptionDto, Subscription>();

            // 🏠 المناطق (Zone)
            CreateMap<Zone, ZoneDto>().ReverseMap();

            // 🍱 الطلبات
            CreateMap<MealRequest, MealRequestDto>()
                .ForMember(dest => dest.Student, opt => opt.MapFrom(src => src.Student))
                .ForMember(dest => dest.Zone, opt => opt.MapFrom(src => src.Zone))
                .ForMember(dest => dest.Subscription, opt => opt.MapFrom(src => src.Subscription))
                .ReverseMap();

            CreateMap<CreateMealRequestDto, MealRequest>();

            // 📦 الدُفعات
            CreateMap<Batch, BatchDto>()
                .ForMember(dest => dest.TotalRequests, opt => opt.MapFrom(src => src.Items.Count))
                .ReverseMap();
            CreateMap<CreateBatchDto, Batch>();

            // 🚴‍♂️ المندوبين
            CreateMap<Courier, CourierDto>().ReverseMap();
            CreateMap<CreateCourierDto, Courier>();

            // 🧾 التسليمات
            CreateMap<DeliveryProof, DeliveryProofDto>().ReverseMap();
            CreateMap<CreateDeliveryProofDto, DeliveryProof>();

            CreateMap<CreateZoneDto, Zone>();
            CreateMap<Zone, ZoneDto>();
        }
    }
}
