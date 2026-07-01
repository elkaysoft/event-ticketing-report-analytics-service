using ETS.Domain.Enums;

namespace ETS.WebApi.DTO
{
    public class NotificationLogFilter : RequestsPagination
    {
        public string? SearchText { get; set; }
        public string? SortField { get; set; }
        public NotificationStatusEnum? Status { get; set; }
    }
}
