namespace NotificationService.Application.Exceptions
{
    public class ProviderException : Exception
    {
        public string ProviderName { get; private init; }

        public ProviderException(string providerName, string message, Exception innerException)
            : base(message, innerException)
        {
            ProviderName = providerName;
        }
    }
}