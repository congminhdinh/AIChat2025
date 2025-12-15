using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentService.Data;
using DocumentService.Dtos;
using DocumentService.Entities;
using DocumentService.Enums;
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
        private readonly Regex _regexChuong = new Regex(@"^Chương\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _regexMuc = new Regex(@"^Mục\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _regexDieu = new Regex(@"^Điều\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public PromptDocumentBusiness(IRepository<PromptDocument> documentRepository, ICurrentUserProvider currentUserProvider, IOptionsMonitor<AppSettings> optionsMonitor, HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger): base(httpClient, appLogger)
        {
            _documentRepository = documentRepository;
            _currentUserProvider = currentUserProvider;
            _appSettings = optionsMonitor.CurrentValue;
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

                memoryStream.Position = 0; // Reset position to read

                // Construct URL from AppSettings
                var uploadUrl = $"{_appSettings.StorageUrl}/web-api/storage/upload-file";

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
            // Open the document from the stream (Editable = true)
            using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, true))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return;

                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    string? styleId = null;

                    if (_regexChuong.IsMatch(text))
                    {
                        styleId = "Heading1";
                    }
                    else if (_regexMuc.IsMatch(text) || _regexDieu.IsMatch(text))
                    {
                        styleId = "Heading2";
                    }

                    if (styleId != null)
                    {
                        // Assign Style to Paragraph
                        var pPr = para.Elements<ParagraphProperties>().FirstOrDefault();
                        if (pPr == null)
                        {
                            pPr = new ParagraphProperties();
                            para.PrependChild(pPr);
                        }
                        pPr.ParagraphStyleId = new ParagraphStyleId() { Val = styleId };
                    }
                }
                // Save changes to the stream
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
                string currentHeading2 = string.Empty;
                var contentParagraphs = new List<string>();

                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    // Check if this is Heading 1 (Chương)
                    if (_regexChuong.IsMatch(text))
                    {
                        // Before updating heading1, flush any accumulated content
                        FlushChunk(chunks, currentHeading1, currentHeading2, contentParagraphs, documentId, fileName);

                        currentHeading1 = text;
                        currentHeading2 = string.Empty; // Reset heading2 when new chapter starts
                        contentParagraphs.Clear();
                    }
                    // Check if this is Heading 2 (Mục or Điều)
                    else if (_regexMuc.IsMatch(text) || _regexDieu.IsMatch(text))
                    {
                        // Before updating heading2, flush any accumulated content
                        FlushChunk(chunks, currentHeading1, currentHeading2, contentParagraphs, documentId, fileName);

                        // If we already have a Heading2 that starts with "Mục" and this is "Điều",
                        // we should append it to form a combined heading
                        if (!string.IsNullOrEmpty(currentHeading2) &&
                            _regexMuc.IsMatch(currentHeading2) &&
                            _regexDieu.IsMatch(text))
                        {
                            currentHeading2 = $"{currentHeading2}\n{text}";
                        }
                        else
                        {
                            currentHeading2 = text;
                        }
                        contentParagraphs.Clear();
                    }
                    // This is content
                    else
                    {
                        // Only accumulate content if we have at least one heading
                        if (!string.IsNullOrEmpty(currentHeading1) || !string.IsNullOrEmpty(currentHeading2))
                        {
                            contentParagraphs.Add(text);
                        }
                    }
                }

                // Don't forget to flush the last chunk
                FlushChunk(chunks, currentHeading1, currentHeading2, contentParagraphs, documentId, fileName);
            }

            return chunks;
        }

        private void FlushChunk(
            List<DocumentChunkDto> chunks,
            string heading1,
            string heading2,
            List<string> contentParagraphs,
            int documentId,
            string fileName)
        {
            // Only create chunk if we have content
            if (contentParagraphs.Count == 0) return;

            var content = string.Join("\n", contentParagraphs);
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
                var downloadUrl = $"{_appSettings.StorageUrl}/web-api/storage/download-file?filePath={document.FilePath}";

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

                // Prepare batch request for EmbeddingService
                var batchRequest = new BatchVectorizeRequestDto
                {
                    Items = chunks.Select(chunk => new VectorizeRequestDto
                    {
                        Text = chunk.FullText,
                        Metadata = new Dictionary<string, object>
                        {
                            { "document_id", chunk.DocumentId },
                            { "file_name", chunk.FileName },
                            { "heading1", chunk.Heading1 },
                            { "heading2", chunk.Heading2 },
                            { "content", chunk.Content },
                            { "tenant_id", tenantId }
                        }
                    }).ToList()
                };

                // Send to EmbeddingService
                var vectorizeUrl = $"{_appSettings.EmbeddingServiceUrl}/vectorize-batch";
                var response = await PostAsync<BatchVectorizeRequestDto, VectorizeResponseDto>(vectorizeUrl, batchRequest);

                if (response?.Success == true)
                {
                    document.Action = DocumentAction.Vectorize_Success;
                    await _documentRepository.UpdateAsync(document);
                    _logger.LogInformation("Successfully vectorized {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);
                    return true;
                }
                else
                {
                    document.Action = DocumentAction.Vectorize_Failed;
                    await _documentRepository.UpdateAsync(document);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error vectorizing document {DocumentId}", documentId);
                document.Action = DocumentAction.Vectorize_Failed;
                await _documentRepository.UpdateAsync(document);
                throw;
            }
        }
    }
}
