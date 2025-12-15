namespace Infrastructure
{

    //class dùng chung
    public class AppSettings
    {
        public string ApiGatewayUrl { get; set; }
        public string DocumentFilePath { get; set; }
        public string EmbeddingServiceUrl { get; set; }
        public string RegexHeading1 { get; set; }
        public string RegexHeading2 { get; set; }
        public string RegexHeading3 { get; set; }
    }
}
