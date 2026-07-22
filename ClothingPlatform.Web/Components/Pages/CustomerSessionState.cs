using ClothingPlatform.DB.AppDbModels;

namespace ClothingPlatform.Web.Components.Pages
{
    public class CustomerSessionState
    {
        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        public void Login(User user)
        {
            CurrentUser = user;
            NotifyStateChanged();
        }

        public void Logout()
        {
            CurrentUser = null;
            NotifyStateChanged();
        }

        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
