using ChatService.Data;
using ChatService.Dtos;
using ChatService.Entities;
using ChatService.Requests;
using ChatService.Specifications;
using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Paging;
using Infrastructure.Web;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace ChatService.Features
{
    public class ChatFeedbackBusiness: BaseHttpClient
    {
        private readonly IRepository<ChatMessage> _messageRepo;
        private readonly IRepository<ChatFeedback> _feedbackRepo;
        private readonly ICurrentUserProvider _currentUserProvider;
        public ChatFeedbackBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IRepository<ChatMessage> messageRepo, IRepository<ChatFeedback> feedbackRepo, ICurrentUserProvider currentUserProvider) : base(httpClient, appLogger)
        {
            _messageRepo = messageRepo;
            _feedbackRepo = feedbackRepo;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<BaseResponse<PaginatedList<ChatFeedbackDto>>> GetListChatFeedback(GetChatFeedbackListRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var feedbackSpec = new ChatFeedbackFilterSpec(tenantId, input.Ratings, input.PageIndex, input.PageSize);
            var feedbacks = await _feedbackRepo.ListAsync(feedbackSpec);
            var countSpec = new ChatFeedbackFilterSpec(tenantId, input.Ratings);
            var count = await _feedbackRepo.CountAsync(countSpec);
            if (!feedbacks.Any())
            {
                var emptyList = new PaginatedList<ChatFeedbackDto>(new List<ChatFeedbackDto>(), count, input.PageIndex, input.PageSize);
                return new BaseResponse<PaginatedList<ChatFeedbackDto>>(emptyList, input.CorrelationId());
            }
            var allMessageIds = feedbacks.Select(f => f.MessageId)
                .Union(feedbacks.Select(f => f.ResponseId))
                .Distinct()
                .ToList();

            var messageSpec = new ChatMessagesByIdsSpec(allMessageIds, tenantId);
            var messages = await _messageRepo.ListAsync(messageSpec);

            var messageDict = messages.ToDictionary(m => m.Id, m => m.Message);
            var dtos = feedbacks.Select(f => new ChatFeedbackDto
            {
                Id = f.Id,
                Ratings = f.Ratings,
                Content = f.Content,
                Message = messageDict.GetValueOrDefault(f.MessageId)
                          ?? "",
                Response = messageDict.GetValueOrDefault(f.ResponseId)
                          ?? "",
                Category = f.Category
            }).ToList();
            var result = new PaginatedList<ChatFeedbackDto>(dtos, count, input.PageIndex, input.PageSize);

            return new BaseResponse<PaginatedList<ChatFeedbackDto>>(result, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> RateChatFeedBack(RateChatFeedbackRequest input)
        {
            var message = await _messageRepo.GetByIdAsync(input.MessageId);
            if(message == null)
            {
                throw new Exception(nameof(input));
            }
            var feedback = await _feedbackRepo.GetByIdAsync(input.Id);
            
            if(feedback == null)
            {
                var newFeedback = new ChatFeedback(message.RequestId, message.Id, input.Ratings, "", "", "");
                await _feedbackRepo.AddAsync(newFeedback);
                return new BaseResponse<int>(newFeedback.Id, input.CorrelationId());
            }
            else
            {
                feedback.Ratings = input.Ratings;
                await _feedbackRepo.UpdateAsync(feedback);
                return new BaseResponse<int>(feedback.Id, input.CorrelationId());
            }

        }

        public async Task<BaseResponse<ChatFeedbackDetailDto>> GetChatFeedbackDetail(GetChatFeedbackByIdRequest input)
        {
            var feedback = await _feedbackRepo.GetByIdAsync(input.Id);
            if (feedback == null)
            {
                throw new Exception("Feedback is null");
            }
            return new BaseResponse<ChatFeedbackDetailDto>(new ChatFeedbackDetailDto(feedback.Id, feedback.Ratings, feedback.Content, feedback.Category), input.CorrelationId());
        }

        public async Task<BaseResponse<int>> CreateChatFeedback(CreateChatFeedbackRequest input)
        {
            var message = await _messageRepo.GetByIdAsync(input.MessageId);
            if (message == null)
            {
                throw new Exception(nameof(input));
            }
            var isAny = await _feedbackRepo.AnyAsync(new ChatFeedbackByMessageSpec(_currentUserProvider.TenantId, input.MessageId));
            if (isAny)
            {
                throw new Exception("Feedback already exist");
            }
            var newFeedback = new ChatFeedback(message.RequestId, message.Id, 0, input.Category, input.Content, "");
            await _feedbackRepo.AddAsync(newFeedback);
            return new BaseResponse<int>(newFeedback.Id, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> UpdateChatFeedback(UpdateChatFeedbackRequest input)
        {
            var feedback = await _feedbackRepo.GetByIdAsync(input.Id);
            if (feedback == null)
            {
                throw new Exception("Feedback is null");
            }
            feedback.Content = input.Content;
            feedback.Category = input.Category;
            await _feedbackRepo.UpdateAsync(feedback);
            return new BaseResponse<int>(feedback.Id, input.CorrelationId());
        }
    }
}
