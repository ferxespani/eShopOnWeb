using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OrderItemsReserver;

public static class OrderItemsReserver
{
    [FunctionName("OrderItemsReserver")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reserve")] HttpRequest req,
        [Blob("order-requests/{DateTime}.json", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")]
        BlobClient outputBlob,
        ILogger log)
    {
        await outputBlob.UploadAsync(req.Body);

        log.LogInformation("Order request was created to blob storage");

        return new OkObjectResult("");
    }
}
