namespace ETS.Domain.AppConfig
{
    public class AppSettings
    {
        public int MaxRetryCount { get; set; }
        public int CoolDownDelay { get; set; }
        public int IdleDelay { get; set; }
        public int MaxBatchSize { get; set; } // Items processsed per single database trip
        public int MaxBatchesPerBurst { get; set; } // Maximum number of consecutive batches allowed (50 items total)
    }
}
