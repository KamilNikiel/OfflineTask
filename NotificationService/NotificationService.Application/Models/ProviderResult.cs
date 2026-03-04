namespace NotificationService.Application.Models
{
    public record ProviderResult
    {
        public bool IsSuccess { get; private init; }

        public string? ErrorMessage { get; private init; }

        public string? ErrorCode { get; private init; }

        private ProviderResult() { }

        public static ProviderResult Success() => new ProviderResult { IsSuccess = true };

        public static ProviderResult Failure(string? errorMessage, string? errorCode) =>
            new ProviderResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
    }
}