using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Auth;
using Microsoft.AspNetCore.Components;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public partial class Login
    {
        [Inject]
        public AppDbContext _db { get; set; }

        public LoginRequest data { get; set; } = new();


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
        private void HandleLogin()
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
            var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == loginEmail.Trim().ToLower() && u.PasswordHash == loginPassword);
            if (user != null)
            {
                Session.Login(user);
                showToast = true;
                StateHasChanged();

                // Redirect after a brief pause to match premium UX
                Task.Delay(1500).ContinueWith(_ =>
                {
                    InvokeAsync(() =>
                    {
                        if (user.Role == "admin")
                        {
                            Nav.NavigateTo("/admin");
                        }
                        else if (user.Role == "staff")
                        {
                            Nav.NavigateTo("/staff");
                        }
                        else
                        {
                            Nav.NavigateTo("/customer");
                        }
                    });
                });
            }
            else
            {
                loginErrorMessage = "Invalid email or password. Please try again.";
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
            else if (_db.Users.Any(u => u.Email.ToLower() == regEmail.Trim().ToLower()))
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
            var newUser = new User
            {
                FirstName = regFirstName,
                LastName = regLastName,
                Email = regEmail.Trim(),
                Address = regAddress,
                PasswordHash = regPassword,
                Role = "customer"
            };
            _db.Users.Add(newUser);
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
    
