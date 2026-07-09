namespace webhook_gateway.Options
{
    /// <summary>
    /// Downstream routing info for one chatbot, bound from the
    /// "ChatbotRouting" section of appsettings.json.
    /// </summary>
    public class ChatbotRoute
    {
        public string BaseUrl { get; set; } = default!;
        public string ShopAssignmentPath { get; set; } = "/api/crm/shop-assignment";
    }
}