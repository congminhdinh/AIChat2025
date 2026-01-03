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
            group.MapPost("/upload-file", async (StorageBusiness storageBusiness, [FromForm]UploadFileSystemRequest input) =>
            {
                
                return await storageBusiness.UploadFileSystem(input);
            }).DisableAntiforgery();

            group.MapGet("/download-file", (StorageBusiness storageBusiness, [FromQuery] string filePath) =>
            {
                var stream = storageBusiness.DownloadFile(filePath);

                if (stream == null)
                {
                    return Results.NotFound(new { Message = "File not found." });
                }
                return Results.File(stream, contentType: "application/octet-stream");
            });

            group.MapPost("/upload-minio-file", async (StorageBusiness storageBusiness, [FromForm]UploadMinioRequest input) =>
            {
                return await storageBusiness.UploadObject(input);
            }).DisableAntiforgery();

            group.MapGet("/download-minio-file", async (StorageBusiness storageBusiness, [FromQuery] string filePath) =>
            {
                var stream = await storageBusiness.DownloadMinioFile(filePath);

                if (stream == null)
                {
                    return Results.NotFound(new { Message = "File not found in MinIO." });
                }
                return Results.File(stream, contentType: "application/octet-stream");
            });

            //group.MapGet("/policy", async (StorageBusiness storageBusiness) =>
            //{
            //    return storageBusiness.SetPolicy("ai-chat-2025");
            //}).AllowAnonymous();
        }
    }
}
