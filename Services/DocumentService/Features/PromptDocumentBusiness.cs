using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentService.Data;
using DocumentService.Dtos;
using DocumentService.Entities;
using DocumentService.Enums;
using Hangfire;
using Infrastructure;
using Infrastructure.Logging;
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

        public async Task<int> HandleAndUploadDocument(IFormFile file)
        {
            var tenantId = _currentUserProvider.TenantId;
            var uploadedBy = _currentUserProvider.Username;
            // 1. Create Initial Entity
            var docEntity = new PromptDocument
            {
                FileName = file.FileName,
                UploadedBy = uploadedBy,
                TenantId = tenantId, // Handle nullable if necessary
                Action = DocumentAction.Upload,
                CreatedAt = DateTime.UtcNow
            };
            await _documentRepository.AddAsync(docEntity);

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                memoryStream.Position = 0;
                StandardizeHeadings(memoryStream);
                docEntity.Action = DocumentAction.Standardization;
                await _documentRepository.UpdateAsync(docEntity);

                memoryStream.Position = 0;
                var uploadUrl = $"{_appSettings.ApiGatewayUrl}/web-api/storage/upload-file";

                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(memoryStream);
                content.Add(fileContent, "File", file.FileName);
                content.Add(new StringContent(file.FileName), "FileName");
                content.Add(new StringContent("documents"), "Directory");

                var response = await PostFormDataAsync<BaseResponse<StringValueDto>>(uploadUrl, content);
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
                _logger.LogError(ex, "Error processing document upload for file {FileName}", file.FileName);
                docEntity.Action = DocumentAction.Vectorize_Failed;
                await _documentRepository.UpdateAsync(docEntity);

                throw;
            }
            return docEntity.Id;
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

        public List<DocumentChunkDto> ExtractHierarchicalChunks(Stream stream, int documentId, string fileName)
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
                        FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName);

                        currentHeading1 = text;
                        currentHeading2 = null;
                        currentHeading3 = string.Empty;
                        contentParagraphs.Clear();
                    }
                    // Check if this is Heading 2 (Section/Mục)
                    else if (_regexHeading2.IsMatch(text))
                    {
                        // Flush any pending chunk before updating Heading2
                        FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName);

                        currentHeading2 = text;
                        currentHeading3 = string.Empty;
                        contentParagraphs.Clear();
                    }
                    // Check if this is Heading 3 (Article/Điều)
                    else if (_regexHeading3.IsMatch(text))
                    {
                        // Flush previous Article chunk before starting new one
                        FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName);

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
                FlushChunk(chunks, currentHeading1, currentHeading2, currentHeading3, contentParagraphs, documentId, fileName);
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
            string fileName)
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
                Heading1 = heading1,
                Heading2 = heading2,
                Content = content,
                FullText = string.Join("\n", fullTextParts),
                DocumentId = documentId,
                FileName = fileName
            };

            chunks.Add(chunk);
        }

        public async Task<bool> VectorizeDocument(int documentId)
        {
            var tenantId = _currentUserProvider.TenantId;
            // Get document from repository
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
                throw new Exception($"Document with ID {documentId} not found");

            // Update status to Vectorize_Start
            document.Action = DocumentAction.Vectorize_Start;
            await _documentRepository.UpdateAsync(document);

            try
            {
                // Construct URL to download the file from Storage Service
                var downloadUrl = $"{_appSettings.ApiGatewayUrl}/web-api/storage/download-file?filePath={document.FilePath}";

                // Download the file
                var fileStream = await GetStreamAsync(downloadUrl);
                if (fileStream == null)
                    throw new Exception("Failed to download file from Storage Service");

                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Extract hierarchical chunks
                var chunks = ExtractHierarchicalChunks(memoryStream, documentId, document.FileName);

                if (chunks.Count == 0)
                {
                    _logger.LogWarning("No chunks extracted from document {DocumentId}", documentId);
                    document.Action = DocumentAction.Vectorize_Failed;
                    await _documentRepository.UpdateAsync(document);
                    return false;
                }
                const int batchSize = 10;
                var batches = new List<List<DocumentChunkDto>>();
                for (int i = 0; i < chunks.Count; i += batchSize)
                {
                    var batch = chunks.Skip(i).Take(batchSize).ToList();
                    batches.Add(batch);
                }

                _logger.LogInformation("Enqueueing {BatchCount} batches for document {DocumentId}", batches.Count, documentId);
                foreach (var batch in batches)
                {
                    _backgroundJobClient.Enqueue<VectorizeBackgroundJob>(
                        job => job.ProcessBatch(batch, tenantId));
                }


                document.Action = DocumentAction.Vectorize_Success;
                await _documentRepository.UpdateAsync(document);
                _logger.LogInformation("Successfully enqueued {BatchCount} batches ({ChunkCount} chunks total) for document {DocumentId}",
                    batches.Count, chunks.Count, documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error vectorizing document {DocumentId}", documentId);
                document.Action = DocumentAction.Vectorize_Failed;
                await _documentRepository.UpdateAsync(document);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            var tenantId = _currentUserProvider.TenantId;

            try
            {
                // Get document from repository
                var document = await _documentRepository.GetByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document with ID {DocumentId} not found", documentId);
                    return false;
                }

                // Delete entity from SQL DB
                await _documentRepository.DeleteAsync(document);
                _logger.LogInformation("Deleted document {DocumentId} from database", documentId);

                // Call Python API to delete vectors from Qdrant
                var deleteUrl = $"{_appSettings.EmbeddingServiceUrl}/api/embeddings/delete";
                var deleteRequest = new
                {
                    source_id = documentId.ToString(),
                    tenant_id = tenantId,
                    type = 1
                };

                var response = await PostAsync<object, object>(deleteUrl, deleteRequest);
                _logger.LogInformation("Successfully deleted vectors for document {DocumentId} from Qdrant", documentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                throw;
            }
        }
    }
}
