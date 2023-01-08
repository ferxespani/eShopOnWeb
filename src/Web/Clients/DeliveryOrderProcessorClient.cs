namespace Microsoft.eShopWeb.Web.Clients;

public class DeliveryOrderProcessorClient : IDeliveryOrderProcessorClient
{
    private readonly HttpClient _httpClient;

    public DeliveryOrderProcessorClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.DELIVERY_ORDER_PROCESSOR);
    }

    public async Task Process(Stream orderInformation)
    {
        await _httpClient.SendAsync(CreateHttpRequestMessage(orderInformation));
    }

    private HttpRequestMessage CreateHttpRequestMessage(Stream orderInformation)
    {
        HttpRequestMessage httpRequest = new()
        {
            RequestUri = new Uri($"{_httpClient.BaseAddress}/process"),
            Method = HttpMethod.Post,
            Content = new StreamContent(orderInformation)
        };

        return httpRequest;
    }
}
