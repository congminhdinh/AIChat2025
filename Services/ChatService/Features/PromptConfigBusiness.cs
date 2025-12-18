using ChatService.Data;
using ChatService.Dtos;
using ChatService.Entities;
using ChatService.Requests;
using ChatService.Specifications;
using Infrastructure;
using Infrastructure.Web;

namespace ChatService.Features
{
    public class PromptConfigBusiness
    {
        private readonly IRepository<PromptConfig> _repository;
        private readonly ICurrentUserProvider _currentUserProvider;

        public PromptConfigBusiness(IRepository<PromptConfig> repository, ICurrentUserProvider currentUserProvider)
        {
            _repository = repository;
            _currentUserProvider = currentUserProvider;
        }

        /// <summary>
        /// Creates a new PromptConfig after checking for duplicate Key.
        /// </summary>
        public async Task<BaseResponse<int>> CreateAsync(CreatePromptConfigRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            // Check for duplicate Key
            var duplicateSpec = new PromptConfigByKeySpec(request.Key, tenantId);
            var existing = await _repository.FirstOrDefaultAsync(duplicateSpec);

            if (existing != null)
            {
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = $"A PromptConfig with Key '{request.Key}' already exists.",
                    Data = 0
                };
            }

            // Create new entity
            var entity = new PromptConfig
            {
                Key = request.Key,
                Value = request.Value,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserProvider.Username
            };

            await _repository.AddAsync(entity);

            return new BaseResponse<int>(entity.Id, request.CorrelationId());
        }

        /// <summary>
        /// Updates an existing PromptConfig after checking ID exists and no duplicate Key.
        /// </summary>
        public async Task<BaseResponse<int>> UpdateAsync(UpdatePromptConfigRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            // Check if ID exists
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null || entity.TenantId != tenantId)
            {
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "PromptConfig not found.",
                    Data = 0
                };
            }

            // Check for duplicate Key (excluding current ID)
            var duplicateSpec = new PromptConfigByKeySpec(request.Key, tenantId, request.Id);
            var duplicate = await _repository.FirstOrDefaultAsync(duplicateSpec);

            if (duplicate != null)
            {
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = $"A PromptConfig with Key '{request.Key}' already exists.",
                    Data = 0
                };
            }

            // Update entity
            entity.Key = request.Key;
            entity.Value = request.Value;
            entity.LastModifiedAt = DateTime.UtcNow;
            entity.LastModifiedBy = _currentUserProvider.Username;

            await _repository.UpdateAsync(entity);

            return new BaseResponse<int>(entity.Id, request.CorrelationId());
        }

        /// <summary>
        /// Deletes a PromptConfig by ID.
        /// </summary>
        public async Task<BaseResponse<int>> DeleteAsync(DeletePromptConfigRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null || entity.TenantId != tenantId)
            {
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "PromptConfig not found.",
                    Data = 0
                };
            }

            await _repository.DeleteAsync(entity);

            return new BaseResponse<int>(request.Id, request.CorrelationId());
        }

        /// <summary>
        /// Gets a list of PromptConfigs with optional keyword filtering.
        /// </summary>
        public async Task<BaseResponse<List<PromptConfigDto>>> GetListAsync(GetListPromptConfiRequest request)
        {
            var tenantId = _currentUserProvider.TenantId;

            var spec = new PromptConfigFilterSpec(request.Key, tenantId);
            var entities = await _repository.ListAsync(spec);

            var dtos = entities.Select(e => new PromptConfigDto
            {
                Id = e.Id,
                Key = e.Key,
                Value = e.Value
            }).ToList();

            return new BaseResponse<List<PromptConfigDto>>(dtos, request.CorrelationId());
        }
    }
}
