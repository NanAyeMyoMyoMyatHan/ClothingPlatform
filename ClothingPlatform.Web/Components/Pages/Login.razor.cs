using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ClothingPlatform.Web.Services;
using Microsoft.JSInterop;

namespace ClothingPlatform.Web.Components.Pages
{
    public partial class Login
    {
        [Inject]
        public IDbContextFactory<AppDbContext> DbFactory { get; set; }
        [Inject]
        public SessionState Session { get; set; }
        [Inject]
        public NavigationManager Nav { get; set; }
        [Inject]
        public HttpClientServices HttpServices { get; set; }
        [Inject]
        public CustomAuthStateProvider AuthStateProvider { get; set; }
        [Inject]
        public Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; }
        [Inject]
        public CustomerSessionState CustomerSession { get; set; }
        [Inject]
        public ServerCookieService ServerCookies { get; set; }
        public AuthRequest data { get; set; } = new();

        protected override void OnInitialized()
        {
            var relativePath = Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');
            if (string.Equals(relativePath, "login", StringComparison.OrdinalIgnoreCase))
            {
                Nav.NavigateTo("/portal-login", replace: true);
            }
        }


        private string currentPanel = "login";
        private bool showToast = false;
        // Login parameters
        private string loginEmail = "";
        private string loginPassword = "";
        private bool showLoginPw = false;
        private string loginErrorMessage = "";
        private string loginSuccessMessage = "";
        private bool isLoginSubmitting = false;
        // Validation flags
        private bool emailInvalid = false;
        private string emailErrorMsg = "";
        private bool pwInvalid = false;
        private string pwErrorMsg = "";
        // Register parameters
        private string regFirstName = "";
        private string regLastName = "";
        private string regEmail = "";
        private string regPhone = "";
        private string regPassword = "";
        private string regAddress = "";
        private string regConfirmPassword = "";
        private bool showRegPw = false;
        private bool showRegConfirmPw = false;
        private string signupErrorMessage = "";
        // Signup Validation flags
        private bool regFirstNameInvalid = false;
        private bool regLastNameInvalid = false;
        private bool regEmailInvalid = false;
        private string regEmailErrorMsg = "";
        private bool regPhoneInvalid = false;
        private bool regAddressInvalid = false;
        private string regAddressErrorMsg = "";
        private bool regPwInvalid = false;
        private string regPwErrorMsg = "";
        private bool regConfirmPwInvalid = false;
        private string regConfirmPwErrorMsg = "";
        private void SwitchTo(string panel)
        {
            currentPanel = panel;
            ClearAllErrors();
            loginErrorMessage = "";
            signupErrorMessage = "";
            loginSuccessMessage = "";
        }
        private void ClearAllErrors()
        {
            emailInvalid = false;
            pwInvalid = false;
            regFirstNameInvalid = false;
            regLastNameInvalid = false;
            regEmailInvalid = false;
            regPhoneInvalid = false;
            regPwInvalid = false;
            regConfirmPwInvalid = false;
        }
        private bool IsValidEmail(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        }
        private async Task HandleLogin()
        {
            if (isLoginSubmitting)
            {
                return;
            }

            ClearAllErrors();
            loginErrorMessage = "";
            bool isValid = true;
            if (string.IsNullOrWhiteSpace(loginEmail))
            {
                emailInvalid = true;
                emailErrorMsg = UiMessages.PortalLogin.LoginEmailRequired;
                isValid = false;
            }
            else if (!IsValidEmail(loginEmail))
            {
                emailInvalid = true;
                emailErrorMsg = UiMessages.PortalLogin.LoginEmailInvalid;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(loginPassword))
            {
                pwInvalid = true;
                pwErrorMsg = UiMessages.PortalLogin.LoginPasswordRequired;
                isValid = false;
            }
            if (!isValid) return;

            isLoginSubmitting = true;
            StateHasChanged();

            try
            {
                var authRequest = new AuthRequest { Email = loginEmail.Trim(), Password = loginPassword };
                var response = await HttpServices.ExecuteAsync<AuthResponse>("api/auth/login", authRequest, EnumHttpMethod.Post);

                if (response != null && !string.IsNullOrWhiteSpace(response.AccessToken))
                {
                    // Notify authentication state provider
                    AuthStateProvider.NotifyUserAuthentication(response.AccessToken);

                    // Clean up legacy localStorage items
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "customerId");
                        await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                    }
                    catch {}

                    // Fetch user details from DB to initialize SessionState
                    await using var db = await DbFactory.CreateDbContextAsync();
                    var user = await db.Users
                        .Include(u => u.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == loginEmail.Trim().ToLower());

                    if (user != null)
                    {
                        var roleName = response.Role.ToLower();

                        // Set HttpOnly cookies server-side (token + userId)
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("authCookieHelper.setAuth", response.AccessToken, user.UserId);
                        }
                        catch {}
                        ServerCookies.SetAuthCookies(response.AccessToken, user.UserId);

                        if (roleName == "customer")
                        {
                            CustomerSession.Login(user);
                            showToast = true;
                            StateHasChanged();
                            await Task.Delay(1500);
                            Nav.NavigateTo("/");
                            return;
                        }

                        Session.AuthToken = response.AccessToken;
                        Session.Login(user, response.Permissions);
                        showToast = true;
                        StateHasChanged();

                        // Redirect based on dynamic role
                        await Task.Delay(1500);
                        if (roleName == "admin" || roleName == "staff")
                        {
                            Nav.NavigateTo("/dashboard");
                        }
                        else
                        {
                            Nav.NavigateTo("/");
                        }
                    }
                    else
                    {
                        loginErrorMessage = UiMessages.PortalLogin.UserLoadFailed;
                    }
                }
                else
                {
                    loginErrorMessage = UiMessages.PortalLogin.InvalidCredentials;
                }
            }
            catch (Exception ex)
            {
                loginErrorMessage = UiMessages.PortalLogin.LoginFailed(ex.Message);
            }
            finally
            {
                isLoginSubmitting = false;
                StateHasChanged();
            }
        }
        private async Task HandleSignup()
        {
            ClearAllErrors();
            signupErrorMessage = "";
            await using var db = await DbFactory.CreateDbContextAsync();
            bool isValid = true;
            if (string.IsNullOrWhiteSpace(regFirstName))
            {
                regFirstNameInvalid = true;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regLastName))
            {
                regLastNameInvalid = true;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regEmail))
            {
                regEmailInvalid = true;
                regEmailErrorMsg = UiMessages.PortalLogin.RegisterEmailRequired;
                isValid = false;
            }
            else if (!IsValidEmail(regEmail))
            {
                regEmailInvalid = true;
                regEmailErrorMsg = UiMessages.PortalLogin.RegisterEmailInvalid;
                isValid = false;
            }
            else if (await db.Users.AnyAsync(u => u.Email.ToLower() == regEmail.Trim().ToLower()))
            {
                regEmailInvalid = true;
                regEmailErrorMsg = UiMessages.PortalLogin.RegisterEmailDuplicate;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regPhone))
            {
                regPhoneInvalid = true;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regAddress))
            {
                regAddressInvalid = true;
                regAddressErrorMsg = UiMessages.PortalLogin.RegisterAddressRequired;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regPassword))
            {
                regPwInvalid = true;
                regPwErrorMsg = UiMessages.PortalLogin.RegisterPasswordRequired;
                isValid = false;
            }
            else if (regPassword.Length < 8)
            {
                regPwInvalid = true;
                regPwErrorMsg = UiMessages.PortalLogin.RegisterPasswordLength;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regConfirmPassword))
            {
                regConfirmPwInvalid = true;
                regConfirmPwErrorMsg = UiMessages.PortalLogin.RegisterConfirmPasswordRequired;
                isValid = false;
            }
            else if (regPassword != regConfirmPassword)
            {
                regConfirmPwInvalid = true;
                regConfirmPwErrorMsg = UiMessages.PortalLogin.RegisterPasswordsMismatch;
                isValid = false;
            }
            if (!isValid) return;

            var customerRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "customer");
            if (customerRole == null)
            {
                signupErrorMessage = UiMessages.PortalLogin.RegisterUnavailable;
                return;
            }
            string SecurePasswordHash = BCrypt.Net.BCrypt.HashPassword(regPassword);
            var newUser = new User
            {
                FirstName = regFirstName,
                LastName = regLastName,
                Email = regEmail.Trim(),
                Address = regAddress,
                PhoneNumber = regPhone,
                PasswordHash = SecurePasswordHash,
                RoleId = customerRole.RoleId,
                CreatedAt = DateTime.Now
            };
            db.Users.Add(newUser);
            await db.SaveChangesAsync();
            // Save email, switch back to login panel and show success alert
            var registeredEmail = regEmail;

            regFirstName = "";
            regLastName = "";
            regEmail = "";
            regPhone = "";
            regPassword = "";
            regConfirmPassword = "";
            SwitchTo("login");
            loginEmail = registeredEmail;
            loginSuccessMessage = UiMessages.PortalLogin.RegisterSuccess(newUser.FirstName);
        }
        private MarkupString HtmlRaw(string value) => new MarkupString(value);
    }
}
