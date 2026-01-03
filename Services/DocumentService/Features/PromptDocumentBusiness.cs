using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentService.Data;
using DocumentService.Dtos;
using DocumentService.Entities;
using DocumentService.Enums;
using DocumentService.Requests;
using DocumentService.Specifications;
using Hangfire;
using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Paging;
using Infrastructure.Web;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace DocumentService.Features
{
    public class PromptDocumentBusiness: BaseHttpClient
    {
        private readonly IRepository<PromptDocument> _documentRepository;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly AppSettings _appSettings;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly Regex _regexHeading1;
        private readonly Regex _regexHeading2;
        private readonly Regex _regexHeading3;

        public PromptDocumentBusiness(IRepository<PromptDocument> documentRepository, ICurrentUserProvider currentUserProvider, IOptionsMonitor<AppSettings> optionsMonitor, HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IBackgroundJobClient backgroundJobClient) : base(httpClient, appLogger)
        {
            _documentRepository = documentRepository;
            _currentUserProvider = currentUserProvider;
            _appSettings = optionsMonitor.CurrentValue;
            _backgroundJobClient = backgroundJobClient;
            _regexHeading1 = new Regex(_appSettings.RegexHeading1, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexHeading2 = new Regex(_appSettings.RegexHeading2, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexHeading3 = new Regex(_appSettings.RegexHeading3, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        private DocumentDto MapToDto(PromptDocument entity)
        {
            return new DocumentDto(
                entity.Id,
                entity.FileName,
                entity.DocumentName,
                entity.FilePath,
                entity.Action,
                entity.LastModifiedAt
            )
            {
                TenantId = entity.TenantId
            };
        }

        public async Task<BaseResponse<DocumentDto>> GetDocumentById(GetDocumentByIdRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var document = await _documentRepository.FirstOrDefaultAsync(
                new DocumentSpecificationById(input.DocumentId, tenantId));

            if (document == null)
                throw new Exception($"Document with ID {input.DocumentId} not found");

            return new BaseResponse<DocumentDto>(MapToDto(document), input.CorrelationId());
        }

        public async Task<BaseResponse<PaginatedList<DocumentDto>>> GetDocumentList(GetDocumentListRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;

            var spec = new DocumentListSpec(
                tenantId, input.FileName, input.UploadedBy,
                input.Action, input.IsApproved, input.PageIndex, input.PageSize);

            var documents = await _documentRepository.ListAsync(spec);

            var countSpec = new DocumentListSpec(
                tenantId, input.FileName, input.UploadedBy, input.Action, input.IsApproved);
            var count = await _documentRepository.CountAsync(countSpec);

            var documentDtos = documents.Select(MapToDto).ToList();
            var paginatedList = new PaginatedList<DocumentDto>(
                documentDtos, count, input.PageIndex, input.PageSize);

            return new BaseResponse<PaginatedList<DocumentDto>>(paginatedList, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> CreateDocument(CreateDocumentRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var uploadedBy = _currentUserProvider.Username;

            var docEntity = new PromptDocument
            {
                FileName = input.File.FileName,
                UploadedBy = uploadedBy,
                TenantId = tenantId,
                Action = DocumentAction.Upload,
                CreatedAt = DateTime.UtcNow,
                DocumentType = input.DocumentType,
                FatherDocumentId = input.FatherDocumentId,
                DocumentName = input.DocumentName
            };

            await _documentRepository.AddAsync(docEntity);

            try
            {
                using var memoryStream = new MemoryStream();
                await input.File.CopyToAsync(memoryStream);

                memoryStream.Position = 0;
                StandardizeHeadings(memoryStream);
                docEntity.Action = DocumentAction.Standardization;
                await _documentRepository.UpdateAsync(docEntity);

                memoryStream.Position = 0;
                var uploadUrl = $"{_appSettings.ApiGatewayUrl}/web-api/storage/upload-minio-file";

                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(memoryStream);
                content.Add(fileContent, "File", input.File.FileName);
                content.Add(new StringContent(input.File.FileName), "FileName");
                content.Add(new StringContent("documents"), "Directory");

                var token = _currentUserProvider.Token;
                if (string.IsNullOrEmpty(token))
                    throw new Exception("Authentication token not found");

                var response = await PostFormDataWithTokenAsync<BaseResponse<StringValueDto>>(uploadUrl, content, token);
                var uploadedPath = response?.Data?.Value;

                if (!string.IsNullOrEmpty(uploadedPath))
                {
                    docEntity.FilePath = uploadedPath;
                    docEntity.LastModifiedAt = DateTime.UtcNow;
                    docEntity.Action = DocumentAction.Upload;
                    await _documentRepository.UpdateAsync(docEntity);
                }
                else
                {
                    throw new Exception("Upload failed: No path returned from Storage Service.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document upload for file {FileName}", input.File.FileName);
                docEntity.Action = DocumentAction.Vectorize_Failed;
                await _documentRepository.UpdateAsync(docEntity);
                throw;
            }

            return new BaseResponse<int>(docEntity.Id, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> UpdateDocument(UpdateDocumentRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var document = await _documentRepository.FirstOrDefaultAsync(
                new DocumentSpecificationById(input.DocumentId, tenantId));

            if (document == null)
                throw new Exception($"Document with ID {input.DocumentId} not found");
            document.DocumentName = input.DocumentName;
            document.FatherDocumentId = input.FatherDocumentId;
            document.DocumentType = input.DocType;
            document.LastModifiedAt = DateTime.UtcNow;
            document.LastModifiedBy = _currentUserProvider.Username;

            await _documentRepository.UpdateAsync(document);

            return new BaseResponse<int>(document.Id, input.CorrelationId());
        }

        public async Task<BaseResponse<bool>> DeleteDocument(DeleteDocumentRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var document = await _documentRepository.FirstOrDefaultAsync(
                new DocumentSpecificationById(input.DocumentId, tenantId));

            if (document == null)
                throw new Exception($"Document with ID {input.DocumentId} not found");

            try
            {
                // Step 1: Delete from SQL (soft delete)
                await _documentRepository.DeleteAsync(document);
                _logger.LogInformation("Deleted document {DocumentId} from database", input.DocumentId);

                // Step 2: Delete from Vector DB (Qdrant) - isolated error handling
                try
                {
                    var deleteUrl = $"{_appSettings.EmbeddingServiceUrl}/api/embeddings/delete";
                    var deleteRequest = new
                    {
                        source_id = input.DocumentId,
                        tenant_id = tenantId,
                        type = 1,
                        collection_name = "vn_law_documents"
                    };

                    await PostAsync<object, object>(deleteUrl, deleteRequest);
                    _logger.LogInformation(
                        "Successfully deleted vectors for document {DocumentId}", input.DocumentId);
                }
                catch (Exception vectorEx)
                {
                    // Log error but DO NOT rollback DB transaction (soft consistency)
                    _logger.LogError(
                        vectorEx,
                        "VECTOR_DELETE_FAILED: Document {DocumentId} deleted from SQL but vector deletion failed. " +
                        "Manual cleanup may be required for tenant {TenantId}",
                        input.DocumentId, tenantId);
                }

                return new BaseResponse<bool>(true, input.CorrelationId());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", input.DocumentId);
                throw;
            }
        }

        public async Task<BaseResponse<bool>> VectorizeDocument(VectorizeDocumentRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var document = await _documentRepository.FirstOrDefaultAsync(
                new DocumentSpecificationById(input.DocumentId, tenantId));

            if (document == null)
                throw new Exception($"Document with ID {input.DocumentId} not found");

            // Fetch parent document name if this is a decree with a parent
            string? fatherDocumentName = null;
            if (document.DocumentType == DocType.NghiDinh && document.FatherDocumentId > 0)
            {
                var parentDocument = await _documentRepository.GetByIdAsync(document.FatherDocumentId);
                if (parentDocument != null)
                {
                    fatherDocumentName = parentDocument.DocumentName;
                }
            }

            document.Action = DocumentAction.Vectorize_Start;
            await _documentRepository.UpdateAsync(document);

            try
            {
                var downloadUrl = $"{_appSettings.ApiGatewayUrl}/web-api/storage/download-minio-file?filePath={document.FilePath}";

                var token = _currentUserProvider.Token;
                if (string.IsNullOrEmpty(token))
                    throw new Exception("Authentication token not found");

                var fileStream = await GetStreamWithTokenAsync(downloadUrl, token);

                if (fileStream == null)
                    throw new Exception("Failed to download file from Storage Service");

                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var chunks = ExtractHierarchicalChunks(memoryStream, input.DocumentId, document.FileName, document.DocumentName, document.DocumentType, fatherDocumentName);

                if (chunks.Count == 0)
                {
                    _logger.LogWarning("No chunks extracted from document {DocumentId}", input.DocumentId);
                    document.Action = DocumentAction.Vectorize_Failed;
                    await _documentRepository.UpdateAsync(document);
                    return new BaseResponse<bool>(false, input.CorrelationId());
                }

                const int batchSize = 10;
                var batches = new List<List<DocumentChunkDto>>();
                for (int i = 0; i < chunks.Count; i += batchSize)
                {
                    batches.Add(chunks.Skip(i).Take(batchSize).ToList());
                }

                foreach (var batch in batches)
                {
                    _backgroundJobClient.Enqueue<VectorizeBackgroundJob>(
                        job => job.ProcessBatch(batch, tenantId));
                }

                document.Action = DocumentAction.Vectorize_Success;
                await _documentRepository.UpdateAsync(document);

                return new BaseResponse<bool>(true, input.CorrelationId());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error vectorizing document {DocumentId}", input.DocumentId);
                document.Action = DocumentAction.Vectorize_Failed;
                await _documentRepository.UpdateAsync(document);
                throw;
            }
        }

        private void StandardizeHeadings(MemoryStream stream)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, true))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return;
                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    string? styleId = null;
                    if (_regexHeading1.IsMatch(text))
                    {
                        styleId = "Heading1";
                    }
                    else if (_regexHeading2.IsMatch(text))
                    {
                        styleId = "Heading2";
                    }
                    else if (_regexHeading3.IsMatch(text))
                    {
                        styleId = "Heading3";
                    }

                    if (styleId != null)
                    {
                        // Apply the heading style to the paragraph
                        // Get existing ParagraphProperties or create new one if it doesn't exist
                        var pPr = para.Elements<ParagraphProperties>().FirstOrDefault();
                        if (pPr == null)
                        {
                            pPr = new ParagraphProperties();
                            para.PrependChild(pPr);
                        }

                        // Set the paragraph style ID to the determined heading level
                        pPr.ParagraphStyleId = new ParagraphStyleId() { Val = styleId };
                    }
                }

                // Save all changes back to the document stream
                doc.MainDocumentPart.Document.Save();
            }
        }

        public List<DocumentChunkDto> ExtractHierarchicalChunks(Stream stream, int documentId, string fileName, string? documentName, DocType documentType, string? fatherDocumentName)
        {
            var chunks = new List<DocumentChunkDto>();

            using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, false))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return chunks;

                string currentHeading1 = string.Empty;
                string? currentHeading2 = null;
                string currentHeading3 = string.Empty;
                var contentParagraphs = new List<string>();

                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    // Check if this is Heading 1 (Chapter/Chương)
                    if (_regexHeading1.IsMatch(text))
                    {
                        // Flush any pending chunk before updating Heading1
                        FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName, documentName, documentType, fatherDocumentName);

                        currentHeading1 = text;
                        currentHeading2 = null;
                        currentHeading3 = string.Empty;
                        contentParagraphs.Clear();
                    }
                    // Check if this is Heading 2 (Section/Mục)
                    else if (_regexHeading2.IsMatch(text))
                    {
                        // Flush any pending chunk before updating Heading2
                        FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName, documentName, documentType, fatherDocumentName);

                        currentHeading2 = text;
                        currentHeading3 = string.Empty;
                        contentParagraphs.Clear();
                    }
                    // Check if this is Heading 3 (Article/Điều)
                    else if (_regexHeading3.IsMatch(text))
                    {
                        // Flush previous Article chunk before starting new one
                        FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName, documentName, documentType, fatherDocumentName);

                        currentHeading3 = text;
                        contentParagraphs.Clear();
                    }
                    // This is body content
                    else
                    {
                        // Only accumulate content if we have a Heading3 (Article)
                        if (!string.IsNullOrEmpty(currentHeading3))
                        {
                            contentParagraphs.Add(text);
                        }
                    }
                }

                // Flush the last chunk
                FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName, documentName, documentType, fatherDocumentName);
            }

            return chunks;
        }

        private void FlushChunk(
            List<DocumentChunkDto> chunks,
            string heading1,
            string? heading2,
            string heading3,
            List<string> contentParagraphs,
            int documentId,
            string fileName,
            string? documentName,
            DocType documentType,
            string? fatherDocumentName)
        {
            // Only create chunk if we have content paragraphs (skip empty chunks)
            if (contentParagraphs.Count == 0) return;

            // Build Content field: Heading3 text + body paragraphs
            var contentParts = new List<string>();
            if (!string.IsNullOrEmpty(heading3))
                contentParts.Add(heading3);
            contentParts.AddRange(contentParagraphs);
            var content = string.Join("\n", contentParts);

            // Build FullText: Heading1 + Heading2 (if exists) + Content
            var fullTextParts = new List<string>();
            if (!string.IsNullOrEmpty(heading1))
                fullTextParts.Add(heading1);
            if (!string.IsNullOrEmpty(heading2))
                fullTextParts.Add(heading2);
            fullTextParts.Add(content);

            var chunk = new DocumentChunkDto
            {
                DocumentName = documentName,
                DocumentType = documentType,
                FatherDocumentName = fatherDocumentName,
                Heading1 = heading1,
                Heading2 = heading2,
                Content = content,
                FullText = string.Join("\n", fullTextParts),
                DocumentId = documentId,
                FileName = fileName
            };

            chunks.Add(chunk);
        }

    }
}
