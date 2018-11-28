using Microsoft.Extensions.Configuration;

namespace Schematic.Core.Mvc
{
    public class SchematicSettings : ISchematicSettings
    {
        public string ApplicationName { get; set; } = "Schematic";
        public string ApplicationDescription { get; set; } = "Schematic data management system";
        public string ApplicationIcon { get; set; }
        public string ApplicationIconStyle { get; set; }
        public double SetPasswordTimeLimitHours { get; set; } = 24;
        public string ContactEmail { get; set; }
        public EmailSettings EmailSettings { get; set; }

        public SchematicSettings(IConfiguration configuration)
        {
            ApplicationName = configuration["Schematic:ApplicationName"] ?? ApplicationName;
            ApplicationDescription = configuration["Schematic:ApplicationDescription"] ?? ApplicationDescription;
            ApplicationIcon = configuration["Schematic:ApplicationIcon"] ?? ApplicationIcon;
            ApplicationIconStyle = configuration["Schematic:ApplicationIconStyle"] ?? ApplicationIconStyle;
            SetPasswordTimeLimitHours = (configuration["Schematic:SetPasswordTimeLimitHours"].HasValue()) 
                ?   double.Parse(configuration["Schematic:SetPasswordTimeLimitHours"])
                :   SetPasswordTimeLimitHours;
            ContactEmail = configuration["Schematic:ContactEmail"];
            EmailSettings = new EmailSettings()
            {
                FromAddress = configuration["Schematic:Email:FromAddress"] ?? ContactEmail,
                FromDisplayName = configuration["Schematic:Email:FromDisplayName"] ?? ApplicationName,
                SMTPHost = configuration["Schematic:Email:SMTPHost"],
                SMTPPort = (int.TryParse(configuration["Schematic:Email:SMTPPort"], out int port))
                    ?   port
                    :   0,
                SMTPUserName = configuration["Schematic:Email:SMTPUserName"],
                SMTPPassword = configuration["Schematic:Email:SMTPPassword"],
                SMTPEnableSSL = (configuration["Schematic:Email:SMTPEnableSSL"] == "true")
                    ?   true
                    :   false
            };
        }
    }
}