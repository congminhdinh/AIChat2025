using DocumentService.Features;
using Infrastructure.Web;

namespace DocumentService.Endpoints
{
    public static class DocumentEndpoint
    {
        public static void MapDocumentEndpoints(this WebApplication app)
        {
            app.MapWebApiGroups();
        }
        static void MapWebApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("document");
            group.MapGet("/ok", () => Results.Ok("Document service is running")).AllowAnonymous();
            group.MapPost("/upload", async (PromptDocumentBusiness documentBusiness, IFormFile file ) =>
            {
                var result = await documentBusiness.HandleAndUploadDocument(file);
                return Results.Ok(result);
            }).DisableAntiforgery();

            group.MapPost("/vectorize/{documentId}", async (PromptDocumentBusiness documentBusiness, int documentId) =>
            {
                var result = await documentBusiness.VectorizeDocument(documentId);
                return Results.Ok(new { success = result, documentId });
            });
        }
    }
}
