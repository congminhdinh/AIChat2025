namespace Infrastructure
{

    //class dùng chung
    public class AppSettings
    {
        public string ApiGatewayUrl { get; set; }

        //Tenant Service Configuration

        public string TenantHash { get; set; }
        // Document Embedding Configuration
        public string DocumentFilePath { get; set; }
        public string EmbeddingServiceUrl { get; set; }
        public string RegexHeading1 { get; set; }
        public string RegexHeading2 { get; set; }
        public string RegexHeading3 { get; set; }

        // RabbitMQ Configuration
        public string RabbitMQEndpoint { get; set; } = "localhost:5672";
        public string RabbitMQUsername { get; set; } = "guest";
        public string RabbitMQPassword { get; set; } = "guest";


        //Minio Configuration
        public string MinioEndpoint { get; set; }
        public string MinioAccessKey { get; set; }
        public string MinioSecretKey { get; set; }
        public string MinioBucket { get; set; }

        //webapp url
        public string WebAppUrl { get; set; }
        public string ImageBaseUrl { get; set; }
    }
}
