namespace ChatService.Dtos
{
    public class ChatFeedbackDto
    {
        public ChatFeedbackDto(string message, string response, short ratings, string content, string referenceDoc)
        {
            Message = message;
            Response = response;
            Ratings = ratings;
            Content = content;
            ReferenceDoc = referenceDoc;
        }

        public string Message { get; set; }
        public string Response { get; set; }
        public short Ratings { get; set; } = 0; //1 like, 2 dislike
        public string Content { get; set; }
        public string ReferenceDoc { get; set; }
    }
}
