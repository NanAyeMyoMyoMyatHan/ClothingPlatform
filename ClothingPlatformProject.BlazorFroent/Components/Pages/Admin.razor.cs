using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.BlazorFroent.Services;
using ClothingPlatformProject.Models.Order;
using Microsoft.AspNetCore.Components;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public partial class Admin
    {
        [Inject]
        public AppDbContext _db { get; set;  }
        private List<OrderHistoryDto> orders { get; set; } = new List<OrderHistoryDto>();
        
        protected override async Task OnInitializedAsync()
        {
            await BindData();
        }
        private decimal TotalRevenue;
        private int TotalOrders;
        private int PendingOrders;
        private string errorMessage = "";
        private async Task BindData()
        {
            try
            {
                var lst = await HttpClientServices.ExecuteAsync<List<OrderHistoryDto>>
            (
                "admin",
                EnumHttpMethod.Get

            );
                orders = lst ?? new();

                TotalRevenue = orders.Sum(x => x.TotalPrice);
                TotalOrders = orders.Count;
                PendingOrders = orders.Count(x => x.OrderStatus == "Pending");
            }
           
            catch (Exception ex)
            {
                errorMessage = "ဒေတာဆွဲယူရာတွင် အဆင်မပြေပါ- " + ex.Message;
                // Log ထုတ်ကြည့်မယ်
                Console.WriteLine(ex.Message);
            }
            
        }
    }
}
