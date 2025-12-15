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
        public bool Success { get; set; }
        public string? PointId { get; set; }
        public int Dimensions { get; set; }
        public string? Collection { get; set; }
        public int? Count { get; set; }
    }
}
