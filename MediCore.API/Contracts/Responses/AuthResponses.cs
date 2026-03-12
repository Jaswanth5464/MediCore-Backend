using Azure.Core;

namespace MediCore.API.Contracts.Responses
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime ExpiresAt { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new() { Success = false, Message = message, Data = default };
    }
}
// Data returned after login or register
//AccessToken → JWT token for authentication
//RefreshToken → token used to generate new access token
//FullName → user name
//Email → user email
//Role → user role
//ExpiresAt → token expiry time
//    {
// "accessToken":"eyJhbGc...",
// "refreshToken":"sdhfsdfh...",
// "fullName":"Jaswanth Kumar",
// "email":"jaswanth@medicore.com",
// "role":"SuperAdmin",
// "expiresAt":"2026-03-06T13:30:00"
//}