using System.Text.Json;

using Ardalis.GuardClauses;

using Azure.Messaging.ServiceBus;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web.Clients;
using Microsoft.eShopWeb.Web.Interfaces;

using Newtonsoft.Json;

namespace Microsoft.eShopWeb.Web.Pages.Basket;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly IBasketService _basketService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orderService;
    private string? _username = null;
    private readonly IBasketViewModelService _basketViewModelService;
    private readonly IAppLogger<CheckoutModel> _logger;
    private readonly IDeliveryOrderProcessorClient _deliveryOrderProcessorClient;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _serviceBusSender;

    public CheckoutModel(IBasketService basketService,
        IBasketViewModelService basketViewModelService,
        SignInManager<ApplicationUser> signInManager,
        IOrderService orderService,
        IAppLogger<CheckoutModel> logger,
        IDeliveryOrderProcessorClient deliveryOrderProcessorClient,
        ServiceBusClient serviceBusClient)
    {
        _basketService = basketService;
        _signInManager = signInManager;
        _orderService = orderService;
        _basketViewModelService = basketViewModelService;
        _logger = logger;
        _deliveryOrderProcessorClient = deliveryOrderProcessorClient;
        _serviceBusClient = serviceBusClient;
        _serviceBusSender = _serviceBusClient.CreateSender(Constants.SERVICE_BUS_QUEUE_NAME);
    }

    public BasketViewModel BasketModel { get; set; } = new BasketViewModel();

    public async Task OnGet()
    {
        await SetBasketModelAsync();
    }

    public async Task<IActionResult> OnPost(IEnumerable<BasketItemViewModel> items)
    {
        var itemsList = items.ToList();

        try
        {
            await SetBasketModelAsync();

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var updateModel = itemsList.ToDictionary(b => b.Id.ToString(), b => b.Quantity);
            await _basketService.SetQuantities(BasketModel.Id, updateModel);

            var order = await _orderService.CreateOrderAsync(
                BasketModel.Id,
                new Address("123 Main St.", "Kent", "OH", "United States", "44240"));

            await _basketService.DeleteBasketAsync(BasketModel.Id);

            var orderItems = itemsList
                .Select(x => new OrderItem { Id = x.Id, Quantity = x.Quantity });

            MemoryStream memoryStream = new();
            await System.Text.Json.JsonSerializer.SerializeAsync(memoryStream, order);
            memoryStream.Seek(0, SeekOrigin.Begin);

            await _deliveryOrderProcessorClient.Process(memoryStream);

            await SendOrderItemsMessage(orderItems);
        }
        catch (EmptyBasketOnCheckoutException emptyBasketOnCheckoutException)
        {
            //Redirect to Empty Basket page
            _logger.LogWarning(emptyBasketOnCheckoutException.Message);
            return RedirectToPage("/Basket/Index");
        }

        return RedirectToPage("Success");
    }

    private async Task SendOrderItemsMessage(IEnumerable<OrderItem> orderItems)
    {
        ServiceBusMessage serviceBusMessage = new(JsonConvert.SerializeObject(orderItems));
        await _serviceBusSender.SendMessageAsync(serviceBusMessage);
    }

    private async Task SetBasketModelAsync()
    {
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        if (_signInManager.IsSignedIn(HttpContext.User))
        {
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(User.Identity.Name);
        }
        else
        {
            GetOrSetBasketCookieAndUserName();
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(_username!);
        }
    }

    private void GetOrSetBasketCookieAndUserName()
    {
        if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
        {
            _username = Request.Cookies[Constants.BASKET_COOKIENAME];
        }
        if (_username != null) return;

        _username = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions();
        cookieOptions.Expires = DateTime.Today.AddYears(10);
        Response.Cookies.Append(Constants.BASKET_COOKIENAME, _username, cookieOptions);
    }
}
