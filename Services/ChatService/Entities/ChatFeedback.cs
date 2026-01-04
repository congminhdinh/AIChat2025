using Infrastructure.Entities;

namespace ChatService.Entities
{
    public class ChatFeedback: TenancyEntity
    {
        public ChatFeedback(int messageId, int responseId, short ratings, string category, string content, int referenceDocId)
        {
            MessageId = messageId;
            ResponseId = responseId;
            Ratings = ratings;
            Category = category;
            Content = content;
            ReferenceDocId = referenceDocId;
        }

        public int MessageId { get; set; }
        public int ResponseId { get; set; }
        public short Ratings { get; set; } = 0; //1 like, 2 dislike
        public string Category { get; set; }
        public string Content { get; set; }
        public int ReferenceDocId { get; set; } = 0;
    }
}
