using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Services
{
    public static class SmsService
    {
        private static readonly string ApiKey     = Utilities.GetValueFromConfiguration("SmsSettings:ApiKey");
        private static readonly string SenderLine = Utilities.GetValueFromConfiguration("SmsSettings:SenderLine");
        private static readonly string BaseUrl    = Utilities.GetValueFromConfiguration("SmsSettings:BaseUrl");

        private static readonly HttpClient _http = new();

        private static readonly JsonSerializerOptions _json = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static async Task SendAsync(string mobile, string message)
        {
            var payload = new
            {
                sending_type = "webservice",
                from_number  = SenderLine,
                message      = message,
                @params      = new { recipients = new[] { mobile } },
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload, _json),
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.TryAddWithoutValidation("Authorization", ApiKey);
            request.Content = content;

            var response = await _http.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"IPPanel SMS error {(int)response.StatusCode}: {body}");
        }
    }
}
