using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Services
{
    public static class SmsService
    {
        private static readonly Logger     _log       = LogManager.GetLogger("SmsService");
        private static readonly string     ApiKey     = Utilities.GetValueFromConfiguration("SmsSettings:ApiKey");
        private static readonly string     SenderLine = Utilities.GetValueFromConfiguration("SmsSettings:SenderLine");
        private static readonly string     BaseUrl    = Utilities.GetValueFromConfiguration("SmsSettings:BaseUrl");
        private static readonly HttpClient _http      = new();

        private static readonly JsonSerializerOptions _json = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly JsonSerializerOptions _jsonPretty = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
        };

        private static readonly string _logDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "SmsLogs");

        private static string TryPrettyJson(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return JsonSerializer.Serialize(doc, _jsonPretty);
            }
            catch { return raw; }
        }

        private static void WriteToFile(string fileName, string content)
        {
            try
            {
                Directory.CreateDirectory(_logDir);
                File.AppendAllText(Path.Combine(_logDir, fileName), content, Encoding.UTF8);
            }
            catch { /* logging must never crash the main flow */ }
        }

        public static async Task SendAsync(string mobile, string message)
        {
            _log.Info("SMS send attempt → mobile={0} url={1} sender={2}", mobile, BaseUrl, SenderLine);
            AppDbLogger.Info("SmsService", "SMS.Attempt",
                $"mobile={mobile} | url={BaseUrl} | sender={SenderLine}");

            var payload = new
            {
                sending_type = "webservice",
                from_number  = SenderLine,
                message      = message,
                @params      = new { recipients = new[] { mobile } },
            };

            var json     = JsonSerializer.Serialize(payload, _json);
            var jsonPretty = JsonSerializer.Serialize(payload, _jsonPretty);
            _log.Info("SMS payload: {0}", json);
            AppDbLogger.Info("SmsService", "SMS.Payload", json);

            var now = DateTime.Now;
            var fileName = $"sms_{now:yyyy-MM-dd}.txt";

            HttpResponseMessage response;
            string body;
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Headers.TryAddWithoutValidation("Authorization", ApiKey);
                request.Content = content;

                response = await _http.SendAsync(request);
                body     = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                var errEntry = $"""
============================================================
[{now:yyyy-MM-dd HH:mm:ss}] SMS EXCEPTION
-------- REQUEST --------
Method : POST
URL    : {BaseUrl}
Headers:
  Content-Type : application/json; charset=utf-8
  Authorization: {ApiKey}
Body:
{jsonPretty}

-------- ERROR --------
{ex.Message}
============================================================

""";
                WriteToFile(fileName, errEntry);
                _log.Error(ex, "SMS HTTP exception → mobile={0}", mobile);
                AppDbLogger.Error("SmsService", "SMS.HttpException",
                    $"mobile={mobile} | {ex.Message}", ex);
                throw;
            }

            var entry = $"""
============================================================
[{now:yyyy-MM-dd HH:mm:ss}] SMS {(response.IsSuccessStatusCode ? "SUCCESS" : "FAILED")}
-------- REQUEST --------
Method : POST
URL    : {BaseUrl}
Headers:
  Content-Type : application/json; charset=utf-8
  Authorization: {ApiKey}
Body:
{jsonPretty}

-------- RESPONSE --------
Status : {(int)response.StatusCode} {response.StatusCode}
Body:
{TryPrettyJson(body)}
============================================================

""";
            WriteToFile(fileName, entry);

            _log.Info("SMS response → status={0} body={1}", (int)response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                _log.Error("SMS failed → status={0} body={1}", (int)response.StatusCode, body);
                AppDbLogger.Error("SmsService", "SMS.Failed",
                    $"mobile={mobile} | status={(int)response.StatusCode} | body={body}");
                throw new Exception($"IPPanel SMS error {(int)response.StatusCode}: {body}");
            }

            AppDbLogger.Info("SmsService", "SMS.Sent",
                $"Success | mobile={mobile} | status={(int)response.StatusCode} | body={body}");
        }
    }
}
