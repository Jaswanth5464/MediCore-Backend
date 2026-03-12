namespace MediCore.API.Contracts.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public List<int> RoleIds { get; set; } = new List<int>();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserListResponse
    {
        public List<UserResponse> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }
}