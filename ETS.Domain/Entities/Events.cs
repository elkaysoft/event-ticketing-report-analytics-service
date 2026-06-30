using ETS.Domain.Common;

namespace ETS.Domain.Entities
{
    public class Events : Entity<Guid>
    {
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Location { get; private set; } = string.Empty;
        public string BannerUrl { get; private set; } = string.Empty;
        public DateTime EventDate { get; private set; }
        public string KickoffTime { get; private set; } = string.Empty;
        public string EndTime { get; private set; } = string.Empty;
    }
}
