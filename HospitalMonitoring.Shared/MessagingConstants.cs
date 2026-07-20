namespace HospitalMonitoring.Shared;

public static class MessagingConstants
{
    public const string AlertExchange = "medical-alerts-exchange";
    public const string AlertQueue = "medical-alerts";
    public const string DeadLetterExchange = "medical-alerts-dlx";
    public const string DeadLetterQueue = "medical-alerts-dlq";
    public const string RoutingKey = "alert.detected";
}
