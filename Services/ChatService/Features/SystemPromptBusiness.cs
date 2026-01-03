using ChatService.Data;
using ChatService.Dtos;
using ChatService.Entities;
using ChatService.Requests;
using ChatService.Specifications;
using Infrastructure;
using Infrastructure.Web;

namespace ChatService.Features
{
    public class SystemPromptBusiness
    {
        private readonly IRepository<SystemPrompt> _repository;
        private readonly ICurrentUserProvider _currentUserProvider;

        public SystemPromptBusiness(IRepository<SystemPrompt> repository, ICurrentUserProvider currentUserProvider)
        {
            _repository = repository;
            _currentUserProvider = currentUserProvider;
        }

        /// <summary>
        /// Creates a new SystemPrompt. If IsActive is true, deactivates all other prompts for the tenant.
        /// </summary>
        public async Task<BaseResponse<int>> CreateAsync(CreateSystemPromptRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            // If creating as active, deactivate all existing prompts
            if (request.IsActive)
            {
                await DeactivateAllPromptsAsync(tenantId);
            }

            // Create new entity
            var entity = new SystemPrompt
            {
                Name = request.Name,
                Content = request.Content,
                Description = request.Description,
                IsActive = request.IsActive,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserProvider.Username
            };

            await _repository.AddAsync(entity);

            return new BaseResponse<int>(entity.Id, request.CorrelationId());
        }

        /// <summary>
        /// Updates an existing SystemPrompt. If IsActive is set to true, deactivates all other prompts.
        /// </summary>
        public async Task<BaseResponse<int>> UpdateAsync(UpdateSystemPromptRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            // Check if ID exists
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null || entity.TenantId != tenantId)
            {
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "SystemPrompt not found.",
                    Data = 0
                };
            }

            // If setting as active, deactivate all other prompts
            if (request.IsActive && !entity.IsActive)
            {
                await DeactivateAllPromptsAsync(tenantId, request.Id);
            }

            // Update entity
            entity.Name = request.Name;
            entity.Content = request.Content;
            entity.Description = request.Description;
            entity.IsActive = request.IsActive;
            entity.LastModifiedAt = DateTime.UtcNow;
            entity.LastModifiedBy = _currentUserProvider.Username;

            await _repository.UpdateAsync(entity);

            return new BaseResponse<int>(entity.Id, request.CorrelationId());
        }

        /// <summary>
        /// Deletes a SystemPrompt by ID.
        /// </summary>
        public async Task<BaseResponse<int>> DeleteAsync(DeleteSystemPromptRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null || entity.TenantId != tenantId)
            {
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "SystemPrompt not found.",
                    Data = 0
                };
            }

            await _repository.DeleteAsync(entity);

            return new BaseResponse<int>(request.Id, request.CorrelationId());
        }

        /// <summary>
        /// Sets a SystemPrompt as active, deactivating all others for the tenant.
        /// </summary>
        public async Task<BaseResponse<int>> SetActiveAsync(SetActiveSystemPromptRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null || entity.TenantId != tenantId)
            {
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "SystemPrompt not found.",
                    Data = 0
                };
            }

            // Deactivate all other prompts
            await DeactivateAllPromptsAsync(tenantId, request.Id);

            // Activate this prompt
            entity.IsActive = true;
            entity.LastModifiedAt = DateTime.UtcNow;
            entity.LastModifiedBy = _currentUserProvider.Username;

            await _repository.UpdateAsync(entity);

            return new BaseResponse<int>(request.Id, request.CorrelationId());
        }

        /// <summary>
        /// Gets a list of SystemPrompts with optional filtering.
        /// </summary>
        public async Task<BaseResponse<List<SystemPromptDto>>> GetListAsync(GetListSystemPromptRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            var spec = new SystemPromptFilterSpec(request.Name, request.IsActive, tenantId);
            var entities = await _repository.ListAsync(spec);

            var dtos = entities.Select(e => new SystemPromptDto
            {
                Id = e.Id,
                Name = e.Name,
                Content = e.Content,
                Description = e.Description,
                IsActive = e.IsActive
            }).ToList();

            return new BaseResponse<List<SystemPromptDto>>(dtos, request.CorrelationId());
        }

        /// <summary>
        /// Gets the active SystemPrompt for the specified tenant.
        /// Returns null if no active prompt exists (fallback mechanism).
        /// </summary>
        public async Task<SystemPromptDto?> GetActiveAsync(int tenantId)
        {
            var spec = new SystemPromptActiveSpec(tenantId);
            var entity = await _repository.FirstOrDefaultAsync(spec);

            if (entity == null)
            {
                return null;
            }

            return new SystemPromptDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Content = entity.Content,
                Description = entity.Description,
                IsActive = entity.IsActive
            };
        }

        /// <summary>
        /// Deactivates all SystemPrompts for a tenant, optionally excluding one.
        /// </summary>
        private async Task DeactivateAllPromptsAsync(int tenantId, int? excludeId = null)
        {
            var spec = new SystemPromptActiveSpec(tenantId);
            var activePrompts = await _repository.ListAsync(spec);

            foreach (var prompt in activePrompts)
            {
                if (!excludeId.HasValue || prompt.Id != excludeId.Value)
                {
                    prompt.IsActive = false;
                    prompt.LastModifiedAt = DateTime.UtcNow;
                    prompt.LastModifiedBy = _currentUserProvider.Username;
                    await _repository.UpdateAsync(prompt);
                }
            }
        }
    }
}
