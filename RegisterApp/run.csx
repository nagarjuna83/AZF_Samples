#r "Microsoft.WindowsAzure.Storage"
#r "System.Web"
using System;
using System.Net;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public class AppRegistryEntry : TableEntity
{
    public AppRegistryEntry(string PK, string RK) {
        this.PartitionKey = PK;
        this.RowKey = RK;
    }

    public AppRegistryEntry() { }

    public string ServerName { get; set; }

    public string ApplicationName {get; set;}

    public string ServerIPAddress { get; set; }

    public int TTL { get; set; }

    public DateTime DateLastSeen { get; set; }

    public string PublicIPAddress {get; set;}
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    string ClientIP = GetClientIp(req);
   
    Dictionary<string, string> QueryString = GetQueryStrings(req);

    string TenantId, AppId, ServerName, ServerIPAddress;

    try {
         TenantId = QueryString["TenantId"];
         AppId = QueryString["ApplicationId"];

         ServerName = QueryString["ServerName"];
         ServerIPAddress = QueryString["ServerIPAddress"];
    } catch {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Bad Request - Missing Data.  Valid Requests require: TenantId, Application Id, ServerName, ServerIPAddress");
    }
   
    int TTL = Convert.ToInt32(QueryString["TTL"]);

    string PK = TenantId + ":" + AppId;

    CloudStorageAccount storageAccount = CloudStorageAccount.Parse
   ("DefaultEndpointsProtocol=https;AccountName=0202170845;AccountKey=avl9TQYc+OturXeFZzXGDsoz+08CfdZM0aaeRLdkt2aNa5J1L2QqROR5lHqdwg5qUDozt+Vi9CP/9v25chWAdw==");

    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

    CloudTable table = tableClient.GetTableReference("Registry");
    table.CreateIfNotExists();

    AppRegistryEntry app = new AppRegistryEntry(PK, ServerName);
    app.ApplicationName = AppId;
    app.ServerName = ServerName;
    app.ServerIPAddress = ServerIPAddress;
    app.TTL = TTL;
    app.DateLastSeen = DateTime.Now;
    app.PublicIPAddress = GetClientIp(req);

    TableOperation insertOperation = TableOperation.InsertOrMerge(app);

    await table.ExecuteAsync(insertOperation);

    TableQuery<AppRegistryEntry> query = new TableQuery<AppRegistryEntry>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PK ));

    var result = table.ExecuteQuery(query);
    return req.CreateResponse(HttpStatusCode.OK, result, "application/json");    
}

public static Dictionary<string, string> GetQueryStrings(this HttpRequestMessage request)
        {
            return request.GetQueryNameValuePairs()
                          .ToDictionary(kv => kv.Key, kv=> kv.Value, StringComparer.OrdinalIgnoreCase);
        }

private static string GetClientIp(this HttpRequestMessage request)
{
    if (request.Properties.ContainsKey("MS_HttpContext"))
    {
        return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
    }

    return null;
}