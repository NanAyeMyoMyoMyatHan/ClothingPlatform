using ClothingPlatform.DB.AppDbModels;
using Microsoft.AspNetCore.Components;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public partial class Login
    {
        [Inject]
        public AppDbContext _db { get; set;  }
        
    }
}
