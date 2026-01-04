namespace WebApp.Models.Chat
{
    public class ChatFeedbackDto
    {
        public ChatFeedbackDto()
        {
            
        }
        public ChatFeedbackDto(int id, string message, string response, short ratings, string content, ChatFeedbackCategory category)
        {
            Id = id;
            Message = message;
            Response = response;
            Ratings = ratings;
            Content = content;
            Category = category;
        }
        public int Id { get; set; }
        public string Message { get; set; }
        public string Response { get; set; }
        public short Ratings { get; set; } = 0; //1 like, 2 dislike
        public string Content { get; set; }
        public ChatFeedbackCategory Category { get; set; }
    }
}
