namespace Microsoft.eShopWeb.Web.Clients;

public class OrderItemsReserverClient : IOrderItemsReserverClient
{
    private readonly HttpClient _httpClient;

    public OrderItemsReserverClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.ORDER_ITEMS_RESERVER);
    }

    public async Task ReserveOrderItems(Stream orderItems)
    {
        await _httpClient.SendAsync(CreateHttpRequestMessage(orderItems));
    }

    private HttpRequestMessage CreateHttpRequestMessage(Stream orderItems)
    {
        HttpRequestMessage httpRequest = new()
        {
            RequestUri = new Uri($"{_httpClient.BaseAddress}/reserve"),
            Method = HttpMethod.Post,
            Content = new StreamContent(orderItems)
        };

        return httpRequest;
    }
}
