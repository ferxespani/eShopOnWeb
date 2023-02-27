using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace OrderItemsReserver;

public static class OrderItemsReserver
{
    [FunctionName("OrderItemsReserver")]
    public static async Task Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBus")] string queueItem,
        [Blob("order-reservations/{DateTime}.json", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")]
        BlobClient outputBlob,
        ILogger log)
    {
        await using Stream stream = await GenerateStreamFromString(queueItem);
        await outputBlob.UploadAsync(stream);

        var resultMessage = $"order {queueItem} was added to blob storage";

        log.LogInformation(resultMessage);
    }

    private static async Task<Stream> GenerateStreamFromString(string input)
    {
        MemoryStream stream = new();
        StreamWriter writer = new(stream);
        await writer.WriteAsync(input);
        await writer.FlushAsync();
        stream.Position = 0;
        return stream;
    }
}
