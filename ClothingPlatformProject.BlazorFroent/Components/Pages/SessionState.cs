using ClothingPlatform.DB.AppDbModels;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public class SessionState
    {
        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        public void Login(User user)
        {
            CurrentUser = user;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}