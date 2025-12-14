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
    }
}
