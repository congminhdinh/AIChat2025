using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
using StorageService.Features;
using StorageService.Requests;

namespace StorageService.Endpoints
{
    public static class StorageEndpoint
    {
        public static void MapStorageEndpoints(this WebApplication app)
        {
            app.MapWebApiGroups();
        }
        static void MapWebApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("storage");
            group.MapPost("/upload-file", async (StorageBusiness storageBusiness, IFormFile File, string? fileName, string? directory) =>
            {
                var input = new UploadFileSystemRequest
                {
                    File = File,
                    FileName = fileName,
                    Directory = directory
                };
                return await storageBusiness.UploadFileSystem(input);
            }).DisableAntiforgery()
            .AllowAnonymous();
        }
    }
}
