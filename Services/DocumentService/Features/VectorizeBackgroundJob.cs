using DocumentService.Dtos;
using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using Microsoft.Extensions.Options;

namespace DocumentService.Features
{
    public class VectorizeBackgroundJob : BaseHttpClient
    {
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly AppSettings _appSettings;

        public VectorizeBackgroundJob(
            HttpClient httpClient,
            IAppLogger<BaseHttpClient> appLogger,
            ICurrentUserProvider currentUserProvider,
            IOptionsMonitor<AppSettings> optionsMonitor)
            : base(httpClient, appLogger)
        {
            _currentUserProvider = currentUserProvider;
            _appSettings = optionsMonitor.CurrentValue;
        }

        public async Task ProcessBatch(List<DocumentChunkDto> chunks, int tenantId)
        {
            try
            {
                var batchRequest = new BatchVectorizeRequestDto
                {
                    Items = chunks.Select(chunk => new VectorizeRequestDto
                    {
                        Text = chunk.FullText,
                        Metadata = new Dictionary<string, object>
                        {
                            { "source_id", chunk.DocumentId },
                            { "file_name", chunk.FileName },
                            { "heading1", chunk.Heading1 },
                            { "heading2", chunk.Heading2 },
                            { "content", chunk.Content },
                            { "tenant_id", tenantId },
                            { "type", 1 }
                        }
                    }).ToList()
                };

                var vectorizeUrl = $"{_appSettings.EmbeddingServiceUrl}/vectorize-batch";
                var response = await PostAsync<BatchVectorizeRequestDto, VectorizeResponseDto>(vectorizeUrl, batchRequest);

                if (response?.Success == true)
                {
                    _logger.LogInformation("Successfully vectorized batch of {ChunkCount} chunks", chunks.Count);
                }
                else
                {
                    _logger.LogError("Failed to vectorize batch of {ChunkCount} chunks", chunks.Count);
                    throw new Exception($"Vectorization failed for batch of {chunks.Count} chunks");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vectorization batch");
                throw;
            }
        }
    }
}
