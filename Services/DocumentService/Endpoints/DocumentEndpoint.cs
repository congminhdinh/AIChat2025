using DocumentService.Enums;
using DocumentService.Features;
using DocumentService.Requests;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace DocumentService.Endpoints
{
    public static class DocumentEndpoint
    {
        public static void MapDocumentEndpoints(this WebApplication app)
        {
            app.MapDocumentApiGroups();
        }

        static void MapDocumentApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("document");

            // Health check
            group.MapGet("/ok", () => Results.Ok("Document service is running"))
                .AllowAnonymous();

            // CRUD Endpoints

            // GET /document/{id}
            group.MapGet("/{id}", async (PromptDocumentBusiness documentBusiness, int id) =>
            {
                return await documentBusiness.GetDocumentById(
                    new GetDocumentByIdRequest { DocumentId = id });
            });

            // GET /document/list
            group.MapGet("/list", async (
                PromptDocumentBusiness documentBusiness,
                [AsParameters] GetDocumentListRequest input) =>
            {
                return await documentBusiness.GetDocumentList(input);
            });

            // POST /document/
            group.MapPost("/", async (
                PromptDocumentBusiness documentBusiness,
                IFormFile file, DocType doctype, int fatherDocumentId, string? documentName) =>
            {
                var request = new CreateDocumentRequest { File = file, DocumentType = doctype, FatherDocumentId = fatherDocumentId, DocumentName = documentName};
                return await documentBusiness.CreateDocument(request);
            }).DisableAntiforgery();

            // PUT /document/
            group.MapPut("/", async (
                PromptDocumentBusiness documentBusiness,
                [FromBody] UpdateDocumentRequest input) =>
            {
                return await documentBusiness.UpdateDocument(input);
            });

            // DELETE /document/{id}
            group.MapDelete("/{id}", async (
                PromptDocumentBusiness documentBusiness,
                int id) =>
            {
                return await documentBusiness.DeleteDocument(
                    new DeleteDocumentRequest { DocumentId = id });
            });

            // POST /document/vectorize/{documentId}
            group.MapPost("/vectorize/{documentId}", async (
                PromptDocumentBusiness documentBusiness,
                int documentId) =>
            {
                return await documentBusiness.VectorizeDocument(
                    new VectorizeDocumentRequest { DocumentId = documentId });
            });

            //group.MapPost("/upload", async (
            //    PromptDocumentBusiness documentBusiness,
            //    IFormFile file) =>
            //{
            //    var request = new CreateDocumentRequest { File = file };
            //    var result = await documentBusiness.CreateDocument(request);
            //    return Results.Ok(result.Data);
            //}).DisableAntiforgery();
        }
    }
}
