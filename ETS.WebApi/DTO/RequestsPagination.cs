using System.ComponentModel.DataAnnotations;

namespace ETS.WebApi.DTO
{
    public class RequestsPagination
    {
        [Range(minimum: 1, maximum: int.MaxValue, ErrorMessage = "Page number must be greater than 0.")]
        public int PageNumber { get; set; } = 1;
        [Range(minimum: 1, maximum: int.MaxValue, ErrorMessage = "Page size must be greater than 0.")]
        public int PageSize { get; set; } = 10;
    }
}
