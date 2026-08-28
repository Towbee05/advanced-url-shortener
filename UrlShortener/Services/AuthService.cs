using System.Net;
using System.Text.Json;
using System.Security.Cryptography;
using BCrypt.Net;
using UrlShortener.Repository;
using UrlShortener.Models;
using UrlShortener.DTO.Response;
using UrlShortener.Entities;
using Microsoft.Extensions.Options;

namespace UrlShortener.Services;

public interface IAuthService
{
    Task<ServiceResult<string>> RegisterAsync(string username, string email, string password);
    Task<ServiceResult<AuthenticationModel>> VerifyEmailAsync(string email, string verificationCode);
    Task<ServiceResult<AuthenticationModel>> LoginAsync(string email, string password);
    Task<ServiceResult<string>> ForgotPasswordAsync(string email);
    Task<ServiceResult<string>> ResetPasswordAsync(string email, string resetPasswordToken, string newPassword, string confirmNewPassword);
    Task<ServiceResult<AuthenticationModel>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult<string>> LogoutAsync (Guid userId, string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly ICacheService _redis;
    private readonly IJwtService _jwt;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _mailer;
    private readonly AppSettings _appSettings;
    private readonly string notVerifiedKey = "AwaitingVerification";
    private readonly string forgotPasswordKey = "ForgotPassword";

    public AuthService(IUserRepository userRepo, ICacheService redis, ILogger<AuthService> logger, IEmailService email, IJwtService jwt, IOptions<AppSettings> appSettings)
    {
        this._userRepository = userRepo;
        this._redis = redis;
        this._logger = logger;
        this._mailer = email;
        this._jwt = jwt;
        this._appSettings = appSettings.Value;
    }

    public async Task<ServiceResult<string>> RegisterAsync(string username, string email, string password)
    {
        try
        {
            // Verify if email exists 
            var existingEmail = await this._userRepository.GetUserByEmailAsync(email);

            if (existingEmail is not null)
            {
                _logger.LogWarning("Register auth Service: failed to register user: duplicate email address for {Email}", email);
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "email address is taken by another user",
                    ErrorCode = (int)HttpStatusCode.BadRequest
                };
            }

            var existingUsername = await this._userRepository.GetUserByUsernameAsync(username);
            if (existingUsername is not null)
            {
                _logger.LogWarning("Register auth Service: failed to register user: duplicate username for {Username}", username);
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "username is taken by another user",
                    ErrorCode = (int)HttpStatusCode.BadRequest
                };
            }
            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Generate verification code
            string verificationCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            // hash the verification code
            string hashedVerificationCode = BCrypt.Net.BCrypt.HashPassword(verificationCode);

            // Save user data into a temporary storage until email verification
            TempUser temporaryUser = new TempUser
            {
                Username = username,
                Email = email,
                Password = hashedPassword,
                HashedVerificationCode = hashedVerificationCode
            };
            // Convert user to string
            string temporaryUserString = JsonSerializer.Serialize(temporaryUser);

            // Add to redis 
            string awaitingVerificationKey = $"{notVerifiedKey}:{email}";

            bool ok = await this._redis.SetStringAsync(awaitingVerificationKey, temporaryUserString, TimeSpan.FromMinutes(15));

            if (!ok)
            {
                _logger.LogWarning("Register auth Service: failed to register user: cache failed to set user details");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "unable to set pending user details in cache",
                    ErrorCode = (int)HttpStatusCode.InternalServerError
                };
            }

            // Send verification email
            VerificationEmailModel verificationModel = new VerificationEmailModel
            {
                Username = username,
                VerificationCode = verificationCode,
                ExpiryMinute = 15
            };

            bool sendVericationMailSuccess = await this._mailer.SendVerificationMailAsync(email, verificationModel);

            if (!sendVericationMailSuccess)
            {
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "failed to send email verification code",
                    ErrorCode = (int)HttpStatusCode.InternalServerError
                };
            }

            this._logger.LogInformation("New user awaiting verification: {Email}", email);
            return new ServiceResult<string>
            {
                Success = true,
                Data = "Account verification code sent to email",
            };
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Register auth service: failed to register new user");
            return new ServiceResult<string>
            {
                Success = false,
                Error = "internal server error",
                ErrorCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<ServiceResult<AuthenticationModel>> VerifyEmailAsync(string email, string verificationCode)
    {
        try
        {
            var key = $"{this.notVerifiedKey}:{email}";
            // Check if email exists in cache via verification email
            string? cachedValue = await this._redis.GetStringAsync(key);
            if (cachedValue is null)
            {
                this._logger.LogWarning("verify email auth service: Failed to find the associated email address: {Email}", email);
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "unable to find the associated email address",
                    ErrorCode = (int)HttpStatusCode.NotFound
                };
            }

            // Verify hash
            TempUser? deserializedTempUser = JsonSerializer.Deserialize<TempUser>(cachedValue);
            if (deserializedTempUser is null)
            {
                this._logger.LogWarning("verify email auth service: Failed to deserialize cached user data into temp user data");
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "unable to deserialize user data string",
                    ErrorCode = (int)HttpStatusCode.UnprocessableEntity
                };
            }
            string hashedVerificationCode = deserializedTempUser.HashedVerificationCode;
            // Compare hashed with raw verification code 
            bool isHashCorrect = BCrypt.Net.BCrypt.Verify(verificationCode, hashedVerificationCode);
            if (!isHashCorrect)
            {
                this._logger.LogWarning("verify email auth service: Failed to verify code, verification code does not match hash");
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "unable to verify code",
                    ErrorCode = (int)HttpStatusCode.BadRequest
                };
            }
            // Verification is complete, add to db, and delete from cache
            var createdUser = await this._userRepository.CreateUserAsync(new User
            {
                Email = deserializedTempUser.Email,
                Username = deserializedTempUser.Username,
                Password = deserializedTempUser.Password,
                IsActive = true,
                IsVerified = true,
                UpdatedAt = DateTime.UtcNow
            });

            this._logger.LogInformation("Successfully Verified code: created new user");

            // Generate access and refresh token
            string accessToken = this._jwt.GenerateAccessToken(createdUser);
            string refreshToken = this._jwt.GenerateRefreshToken();
            bool ok = await this._jwt.StoreRefreshTokenAsync(createdUser.Id, refreshToken);
            if (!ok)
            {
                this._logger.LogWarning("verify email auth service: unable to store referesh token in cache");
            }
            // Delete verification key
            bool deleted = await this._redis.DeleteStringAsync(key);
            if (!deleted)
            {
                this._logger.LogWarning("verify email auth service: failed to delete verification key from cache");
            }
            return new ServiceResult<AuthenticationModel>
            {
                Success = true,
                Data = new AuthenticationModel
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "failed to verify new user");
            return new ServiceResult<AuthenticationModel>
            {
                Success = false,
                Error = "internal server error",
                ErrorCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
    public async Task<ServiceResult<AuthenticationModel>> LoginAsync(string email, string password)
    {
        try
        {
            // Verify if email exists 
            var user = await this._userRepository.GetUserByEmailAsync(email);
            if (user is null)
            {
                _logger.LogWarning("Login auth service: failed to login user: duplicate email address for {Email}", email);
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "incorrect email or password",
                    ErrorCode = (int)HttpStatusCode.BadRequest
                };
            }

            // Verify password
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (!isPasswordCorrect)
            {
                _logger.LogWarning("Login auth service: failed to authenticate user: password is incorrect");
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "incorrect email or password",
                    ErrorCode = (int)HttpStatusCode.BadRequest
                };
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login auth service: failed to log user in: user is currently deactivated");
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "user account is currently disabled",
                    ErrorCode = (int)HttpStatusCode.Unauthorized
                };
            }

            // issue access token and refresh token
            string accessToken = this._jwt.GenerateAccessToken(user);
            string refreshToken = this._jwt.GenerateRefreshToken();

            // store refresh token
            bool ok = await this._jwt.StoreRefreshTokenAsync(user.Id, refreshToken);
            if (!ok)
            {
                this._logger.LogWarning("Login auth service: failed to store refresh token in cache");
            }
            return new ServiceResult<AuthenticationModel>
            {
                Success = true,
                Data = new AuthenticationModel
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }
            };

        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "failed to login user");
            return new ServiceResult<AuthenticationModel>
            {
                Success = false,
                Error = "internal server error",
                ErrorCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<ServiceResult<string>> ForgotPasswordAsync(string email)
    {
        try
        {
            // Verify if email exists
            var user = await this._userRepository.GetUserByEmailAsync(email);
            if (user is null)
            {
                _logger.LogWarning("Forgot password auth service: incoming email address does not exist in database, {Email}", email);
                return new ServiceResult<string>
                {
                    Success = true,
                    Data = "Password reset link sent to associated email address"
                };
            }

            // generate and hash reset token
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            int resetLinkExpiry = 15;
            string generatedResetToken = Uri.EscapeDataString(Convert.ToBase64String(randomBytes));
            string hashedGeneratedResetToken = BCrypt.Net.BCrypt.HashPassword(generatedResetToken);

            // store hashed generatedResetToken in cahe
            bool ok = await this._redis.SetStringAsync($"{this.forgotPasswordKey}:{email}", hashedGeneratedResetToken, TimeSpan.FromMinutes(resetLinkExpiry));
            if (!ok)
            {
                _logger.LogWarning("Forgot password auth service: failed to set generated reset token to cache");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "internal server error",
                    ErrorCode = (int)HttpStatusCode.InternalServerError
                };
            }

            string resetLink = $"{this._appSettings.FrontendBaseURL}/reset-password/?email={Uri.EscapeDataString(email)}&token={generatedResetToken}";

            ForgotPasswordEmailModel resetPasswordModel = new ForgotPasswordEmailModel
            {
                Username = user.Username,
                ResetLink = resetLink,
                ExpiryMinute = resetLinkExpiry
            };

            // Send email to user
            bool emailSent = await this._mailer.SendPasswordResetMailAsync(email, resetPasswordModel);
            if (!emailSent)
            {
                _logger.LogWarning("Forgot password auth service: failed to send password reset mail");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "internal server error",
                    ErrorCode = (int)HttpStatusCode.InternalServerError
                };
            }

            this._logger.LogInformation("Forgot password auth service: sent password reset link to user email, {Email}", email);
            return new ServiceResult<string>
            {
                Success = true,
                Data = "Password reset link sent to associated email address"
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Forgot password auth service: failed to send password reset link to user");
            return new ServiceResult<string>
            {
                Success = false,
                Error = "internal server error",
                ErrorCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<ServiceResult<string>> ResetPasswordAsync(string email, string resetPasswordToken, string newPassword, string confirmNewPassword)
    {
        try
        {
            // Check if email exists
            var user = await this._userRepository.GetUserByEmailAsync(email);
            if (user is null)
            {
                _logger.LogWarning("Forgot password auth service: incoming email address does not exist in database, {Email}", email);
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "invalid email address",
                    ErrorCode = (int)HttpStatusCode.Unauthorized
                };
            }

            // fetch hashed resePasswordToken from cache (may not exist due to redis ttl)
            string? hashedResetToken = await this._redis.GetStringAsync($"{this.forgotPasswordKey}:{email}");

            if (hashedResetToken is null)
            {
                this._logger.LogWarning("Forgot password auth service: unable fetch email reset token from cache");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "failed to fetch reset token, generate a new token",
                    ErrorCode = (int)HttpStatusCode.Unauthorized
                };
            }

            // check if new password matched confirmed password
            if (newPassword != confirmNewPassword)
            {
                this._logger.LogWarning("Forgot password auth service: new and confirmed password do not match");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "new password does not match confirm password",
                    ErrorCode = (int)HttpStatusCode.BadRequest
                };
            }

            // Verify hashed reset token
            bool ok = BCrypt.Net.BCrypt.Verify(resetPasswordToken, hashedResetToken);
            if (!ok)
            {
                this._logger.LogWarning("Forgot password auth service: submitted reset password token does not match cached reset token");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "invalid reset token",
                    ErrorCode = (int)HttpStatusCode.Unauthorized
                };

            }

            // hash new password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            User? updatedUser = await this._userRepository.UpdatePasswordByEmailAsync(email, hashedPassword, DateTime.UtcNow);

            // Delete reset token from cache
            await this._redis.DeleteStringAsync($"{this.forgotPasswordKey}:{email}");
            return new ServiceResult<string>
            {
                Success = true,
                Data = "password reset successfully"
            };

        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Reset password auth service: failed to send password reset link to user");
            return new ServiceResult<string>
            {
                Success = false,
                Error = "internal server error",
                ErrorCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<ServiceResult<AuthenticationModel>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // check if refresh token exist in cache
            Guid? userId = await this._jwt.ValidateRefreshTokenAsync(refreshToken);

            if (userId is null)
            {
                this._logger.LogWarning("Refresh token auth service: requested refresh token not found in cache");
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "refresh token was not found",
                    ErrorCode = (int)HttpStatusCode.NotFound
                };
            }
            User? user = await this._userRepository.GetUserByIdAsync(userId.Value);

            if (user is null)
            {
                this._logger.LogWarning("Refresh token auth service: unable to get requested user");
                return new ServiceResult<AuthenticationModel>
                {
                    Success = false,
                    Error = "refresh token was not found",
                    ErrorCode = (int)HttpStatusCode.NotFound
                };
            }
            
            // Generatr new access and new refresh token
            string accessToken = this._jwt.GenerateAccessToken(user);
            string newRefreshToken = this._jwt.GenerateRefreshToken();

            // add new refresh token
            await this._jwt.StoreRefreshTokenAsync(user.Id, newRefreshToken);

            this._logger.LogInformation("Refresh token auth service: successfully refreshed access token");
            return new ServiceResult<AuthenticationModel>
            {
                Success = true,
                Data = new AuthenticationModel
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken
                }
            };
        } catch (Exception ex)
        {
            this._logger.LogError(ex, "Reset password auth service: failed to send password reset link to user");
            return new ServiceResult<AuthenticationModel>
            {
                Success = false,
                Error = "internal server error",
                ErrorCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<ServiceResult<string>> LogoutAsync (Guid userId, string refreshToken)
    {
        try
        {
            // check if refresh token exist in cache
            Guid? cachedUserId = await this._jwt.ValidateRefreshTokenAsync(refreshToken);

            if (cachedUserId is null)
            {
                this._logger.LogWarning("Logout auth service: requested refresh token not found in cache");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "refresh token was not found",
                    ErrorCode = (int)HttpStatusCode.NotFound
                };
            }

            if (cachedUserId != userId)
            {
                this._logger.LogWarning("Logout auth service: requested user id does not match cached user id");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "user is unable to perform request",
                    ErrorCode = (int)HttpStatusCode.Unauthorized
                };
            }
            User? user = await this._userRepository.GetUserByIdAsync(cachedUserId.Value);

            if (user is null)
            {
                this._logger.LogWarning("Logout auth service: unable to get requested user");
                return new ServiceResult<string>
                {
                    Success = false,
                    Error = "refresh token was not found",
                    ErrorCode = (int)HttpStatusCode.NotFound
                };
            }
            return new ServiceResult<string>
            {
                Success = true,
                Data = "user has successfully logged out"
            };
        } catch (Exception ex)
        {
            this._logger.LogError(ex, "Logout auth service: failed to send password reset link to user");
            return new ServiceResult<string>
            {
                Success = false,
                Error = "internal server error",
                ErrorCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}