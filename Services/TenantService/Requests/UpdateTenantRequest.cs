namespace TenantService.Requests
{
    public class UpdateTenantRequest: BaseRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
