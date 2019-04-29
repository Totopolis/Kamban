using AutoMapper;
using Kamban.Repository;
using Kamban.ViewModels.Core;

namespace Kamban
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<BoardViewModel, Board>();
            CreateMap<Board, BoardViewModel>();

            CreateMap<CardViewModel, Card>()
                .ForMember(dst => dst.Head, opt => opt.MapFrom(src => src.Header))
                .ForMember(dst => dst.ColumnId, opt => opt.MapFrom(src => src.ColumnDeterminant))
                .ForMember(dst => dst.RowId, opt => opt.MapFrom(src => src.RowDeterminant));

            CreateMap<Card, CardViewModel>()
                .ForMember(dst => dst.Header, opt => opt.MapFrom(src => src.Head))
                .ForMember(dst => dst.ColumnDeterminant, opt => opt.MapFrom(src => src.ColumnId))
                .ForMember(dst => dst.RowDeterminant, opt => opt.MapFrom(src => src.RowId));

            CreateMap<ColumnViewModel, Column>()
                // incorrect .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Determinant))
                .ForMember(dst => dst.Width, opt => opt.MapFrom(src => src.Size));

            CreateMap<Column, ColumnViewModel>()
                .ForMember(dst => dst.Size, opt => opt.MapFrom(src => src.Width));

            CreateMap<RowViewModel, Row>()
                // incorrect: .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Determinant))
                .ForMember(dst => dst.Height, opt => opt.MapFrom(src => src.Size));

            CreateMap<Row, RowViewModel>()
                .ForMember(dst => dst.Size, opt => opt.MapFrom(src => src.Height));
        }
    }
}
