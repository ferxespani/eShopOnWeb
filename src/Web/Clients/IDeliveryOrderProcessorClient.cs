namespace Microsoft.eShopWeb.Web.Clients;

public interface IDeliveryOrderProcessorClient
{
    Task Process(Stream orderInformation);
}
