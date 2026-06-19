using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ClothingPlatformProject.BlazorFroent.Services;
using Microsoft.JSInterop;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public partial class Login
    {
        [Inject]
        public AppDbContext _db { get; set; }
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
        public AuthRequest data { get; set; } = new();


        private string currentPanel = "login";
        private bool showToast = false;
        // Login parameters
        private string loginEmail = "";
        private string loginPassword = "";
        private bool showLoginPw = false;
        private string loginErrorMessage = "";
        private string loginSuccessMessage = "";
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
            ClearAllErrors();
            loginErrorMessage = "";
            bool isValid = true;
            if (string.IsNullOrWhiteSpace(loginEmail))
            {
                emailInvalid = true;
                emailErrorMsg = "Email address is required.";
                isValid = false;
            }
            else if (!IsValidEmail(loginEmail))
            {
                emailInvalid = true;
                emailErrorMsg = "Please enter a valid email address.";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(loginPassword))
            {
                pwInvalid = true;
                pwErrorMsg = "Password is required.";
                isValid = false;
            }
            if (!isValid) return;

            try
            {
                var authRequest = new AuthRequest { Email = loginEmail.Trim(), Password = loginPassword };
                var response = await HttpServices.ExecuteAsync<AuthResponse>("api/auth/login", authRequest, EnumHttpMethod.Post);

                if (response != null && !string.IsNullOrWhiteSpace(response.AccessToken))
                {
                    // Save token to localStorage
                    await JSRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", response.AccessToken);

                    // Notify authentication state provider
                    AuthStateProvider.NotifyUserAuthentication(response.AccessToken);

                    // Fetch user details from DB to initialize SessionState
                    var user = _db.TblUsers
                        .Include(u => u.Role)
                            .ThenInclude(r => r.TblRolePermissions)
                                .ThenInclude(rp => rp.Permission)
                        .FirstOrDefault(u => u.Email.ToLower() == loginEmail.Trim().ToLower());

                    if (user != null)
                    {
                        Session.Login(user, response.Permissions);
                        showToast = true;
                        StateHasChanged();

                        // Redirect based on dynamic role
                        await Task.Delay(1500);
                        var roleName = response.Role.ToLower();
                        if (roleName == "admin" )
                        {
                            Nav.NavigateTo("/dashboard");
                        }
                        else if(roleName == "staff")
                        {
                            Nav.NavigateTo("/staff");
                        }
                        else
                        {
                            Nav.NavigateTo("/customer");
                        }
                    }
                    else
                    {
                        loginErrorMessage = "User details could not be loaded from database.";
                    }
                }
                else
                {
                    loginErrorMessage = "Invalid email or password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                loginErrorMessage = $"Login error: {ex.Message}";
            }
        }
        private void HandleSignup()
        {
            ClearAllErrors();
            signupErrorMessage = "";
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
                regEmailErrorMsg = "Email address is required.";
                isValid = false;
            }
            else if (!IsValidEmail(regEmail))
            {
                regEmailInvalid = true;
                regEmailErrorMsg = "Please enter a valid email address.";
                isValid = false;
            }
            else if (_db.TblUsers.Any(u => u.Email.ToLower() == regEmail.Trim().ToLower()))
            {
                regEmailInvalid = true;
                regEmailErrorMsg = "An account with this email already exists.";
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
                regAddressErrorMsg = "Address is required.";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regPassword))
            {
                regPwInvalid = true;
                regPwErrorMsg = "Password is required.";
                isValid = false;
            }
            else if (regPassword.Length < 8)
            {
                regPwInvalid = true;
                regPwErrorMsg = "Password must be at least 8 characters.";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(regConfirmPassword))
            {
                regConfirmPwInvalid = true;
                regConfirmPwErrorMsg = "Please confirm your password.";
                isValid = false;
            }
            else if (regPassword != regConfirmPassword)
            {
                regConfirmPwInvalid = true;
                regConfirmPwErrorMsg = "Passwords do not match.";
                isValid = false;
            }
            if (!isValid) return;

            // ✅ Dynamic RBAC: Look up "customer" role from TblRoles
            var customerRole = _db.TblRoles.FirstOrDefault(r => r.RoleName.ToLower() == "customer");
            if (customerRole == null)
            {
                signupErrorMessage = "Registration is currently unavailable. Please try again later.";
                return;
            }
            string SecurePasswordHash = BCrypt.Net.BCrypt.HashPassword(regPassword);
            var newUser = new TblUser
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
            _db.TblUsers.Add(newUser);
            _db.SaveChanges();
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
            loginSuccessMessage = $"Welcome to The Boutique, <strong>{newUser.FirstName}</strong>! Your membership has been created. Please sign in.";
        }
        private MarkupString HtmlRaw(string value) => new MarkupString(value);
    }
}
