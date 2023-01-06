namespace Microsoft.eShopWeb.Web.Clients;

public interface IOrderItemsReserverClient
{
    public Task ReserveOrderItems(Stream orderItems);
}
