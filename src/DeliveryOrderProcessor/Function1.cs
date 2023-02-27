using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DeliveryOrderProcessor;

public static class DeliveryOrderProcessor
{
    [FunctionName("DeliveryOrderProcessor")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "process")] HttpRequest req,
        [CosmosDB(
            databaseName: "Orders",
            collectionName: "Items",
            ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<dynamic> document,
        ILogger log)
    {
        log.LogInformation("Order processing started.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject<dynamic>(requestBody);
        await document.AddAsync(data);

        log.LogInformation("Order processing finished.");

        return new JsonResult(data)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }
}
