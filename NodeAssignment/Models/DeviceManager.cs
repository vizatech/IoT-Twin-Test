using System.Configuration;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Management.IotHub;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace NodeAssignment.Models
{
    public class DeviceManager
    {
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string secret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string subscriptionId = ConfigurationManager.AppSettings["ida:SubscriptionId"];

        private IAzure _azure;
        private ResourceManagementClient _rmClient;
        private IotHubClient _hubClient;

        public DeviceManager()
        {
            AzureCredentials creds = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                clientId,
                secret,
                tenantId,
                AzureEnvironment.AzureGlobalCloud);

            // create clients
            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(creds)
                .WithSubscription(subscriptionId);

            _hubClient = new IotHubClient(creds) { SubscriptionId = subscriptionId };
            _rmClient = new ResourceManagementClient(creds) { SubscriptionId = subscriptionId };
        }

        public IAzure azureClient()
        {
            return _azure;
        }

        public IResourceManagementClient rmClient()
        {
            return _rmClient;
        }

        public IotHubClient hubClient()
        {
            return _hubClient;
        }

        public RegistryManager registryMangerForHub(string hub, string group)
        {
            // create hub connection string
            var keyName = "iothubowner";
            var key = _hubClient.IotHubResource.GetKeysForKeyName(group, hub, keyName);
            var connStr = string.Format("HostName={0}.azure-devices.net;SharedAccessKeyName={1};SharedAccessKey={2}", hub, keyName, key.PrimaryKey);
            var registryManager = RegistryManager.CreateFromConnectionString(connStr);

            return registryManager;
        }
    }
}