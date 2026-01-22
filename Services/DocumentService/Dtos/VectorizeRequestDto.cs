namespace DocumentService.Dtos
{
    public class VectorizeRequestDto
    {
        public string Text { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public string? CollectionName { get; set; }
    }

    public class BatchVectorizeRequestDto
    {
        public List<VectorizeRequestDto> Items { get; set; } = new List<VectorizeRequestDto>();
        public string? CollectionName { get; set; }
    }

    public class VectorizeResponseDto
    {
        public bool success { get; set; }
        public string? point_id { get; set; }
        public string? dimensions { get; set; }
        public string? collection { get; set; }
        public int? count { get; set; }
    }
}
