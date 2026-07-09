using System.ComponentModel.DataAnnotations;

namespace webhook_gateway.Models
{
    /// <summary>
    /// Payload sent by the CRM when assigning/updating a shop code for a chatbot.
    /// New chatbots can be onboarded purely via configuration — this model
    /// never needs to change.
    /// </summary>
    public class CrmShopAssignmentRequest
    {
        /// <summary>
        /// Identifies which chatbot should receive this request.
        /// Must match a key under "ChatbotRouting" in appsettings.json
        /// (e.g. "UAE_Chatbot", "KSA_Chatbot", "Malaysia_Chatbot").
        /// </summary>
        [Required]
        public string ChatbotName { get; set; } = default!;

        [Required]
        public string ShopCode { get; set; } = default!;

        /// <summary>
        /// WhatsApp phone number identifying the session to update
        /// (e.g. "971581260024"). Required — the chatbot backend needs this
        /// to know which conversation the shop verification belongs to.
        /// </summary>
        [Required]
        public string Phone { get; set; } = default!;

        /// <summary>
        /// Optional bag for any extra fields the CRM sends that a specific
        /// chatbot might need, without requiring a model change every time
        /// a new field shows up.
        /// </summary>
        public Dictionary<string, string>? AdditionalParameters { get; set; }
    }
}