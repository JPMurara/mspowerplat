using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        if (this.Context.OperationId == "CleanPhone")
        {
            var body = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
            var json = JObject.Parse(body);
            var phone = (string?)json["phone"] ?? string.Empty;
            // Remove all non-digits
            var digitsOnly = Regex.Replace(phone, @"\D+", "");
            var result = new JObject { ["clean"] = digitsOnly };
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = ScriptBase.CreateJsonContent(result.ToString());
            return response;
        }
        var bad = new HttpResponseMessage(HttpStatusCode.BadRequest);
        bad.Content = ScriptBase.CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'");
        return bad;
    }
}
