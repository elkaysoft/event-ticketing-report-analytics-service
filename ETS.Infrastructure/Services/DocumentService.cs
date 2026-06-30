using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ETS.Domain.Contracts;
using Microsoft.AspNetCore.Http;

namespace ETS.Infrastructure.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly Cloudinary _cloudinary;

        public DocumentService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public string UploadDocument(byte[] doc)
        {
            return UploadResult(doc);
        }

        private string UploadResult(byte[] file)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var stream = new MemoryStream(file);
            string fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}{Guid.NewGuid().ToString("N")}";
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileName, stream),
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };
            var uploadResult = _cloudinary.Upload(uploadParams);
            return uploadResult?.SecureUrl.ToString() ?? "";
        }

    }
}
