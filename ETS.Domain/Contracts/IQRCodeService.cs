namespace ETS.Domain.Contracts
{
    public interface IQRCodeService
    {
        byte[] GenerateQRCodeBytes(string payload);
    }
}
