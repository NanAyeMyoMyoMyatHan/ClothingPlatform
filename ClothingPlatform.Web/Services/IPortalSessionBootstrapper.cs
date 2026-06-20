namespace ClothingPlatform.Web.Services
{
    public interface IPortalSessionBootstrapper
    {
        Task<bool> RestorePortalSessionAsync();
    }
}
