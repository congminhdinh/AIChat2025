namespace ChatService.Dtos
{
    public class ChatFeedbackDetailDto
    {
        public ChatFeedbackDetailDto()
        {

        }
        public ChatFeedbackDetailDto(int id, short ratings, string content, ChatFeedbackCategory category)
        {
            Id = id;
            Ratings = ratings;
            Content = content;
            Category = category;
        }

        public int Id { get; set; }
        public short Ratings { get; set; }
        public string Content { get; set; }
        public ChatFeedbackCategory Category { get; set; }
    }
}
