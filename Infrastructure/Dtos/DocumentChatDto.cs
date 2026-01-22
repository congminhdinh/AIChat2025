namespace Infrastructure.Dtos
{
    public record DocumentChatDto
    {
        public DocumentChatDto(int id, string? documentName, string filePath)
        {
            Id = id;
            DocumentName = documentName;
            FilePath = filePath;
        }

        public int Id { get; set; }
        public string? DocumentName { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}
