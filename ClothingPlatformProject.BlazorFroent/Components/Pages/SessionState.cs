using ClothingPlatform.DB.AppDbModels;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public class SessionState
    {
        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;
      //  public string Role => CurrentUser?.Role.RoleName ?? "";

        public void Login(User user)
        {
            CurrentUser = user;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
        //public bool isInRole (string role)
        //{
        //    return Role.Equals(role, StringComparison.OrdinalIgnoreCase);
        //}
    }
}