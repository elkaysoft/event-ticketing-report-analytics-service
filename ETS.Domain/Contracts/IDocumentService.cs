using Microsoft.AspNetCore.Http;

namespace ETS.Domain.Contracts
{
    public interface IDocumentService
    {
        string UploadDocument(byte[] doc);
    }
}
