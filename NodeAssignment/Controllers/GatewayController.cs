using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using NodeAssignment.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace NodeAssignment.Controllers
{
    public class GatewayController : Controller
    {
        DeviceManager _dm;

        public GatewayController()
        {
            _dm = new DeviceManager();
        }

        public async Task<ActionResult> Index()
        {
            var resourceGroups = await _dm.azureClient().ResourceGroups.ListAsync();
            var hubsByGroups = new Dictionary<IResourceGroup, List<string>> { };

            foreach (var group in resourceGroups)
            {
                var hubs = await _dm.rmClient().ResourceGroups.ListResourcesAsync(group.Name, "resourceType eq 'Microsoft.Devices/IotHubs'");

                foreach (var hub in hubs)
                {
                    if (!hubsByGroups.ContainsKey(group))
                    {
                        hubsByGroups.Add(group, new List<string>());
                    }

                    hubsByGroups[group].Add(hub.Name);
                }
            }
            
            return View(hubsByGroups);
        }

        public async Task<ActionResult> List(string hub, string group)
        {
            // find all gateways
            var registryManager = _dm.registryMangerForHub(hub, group);
            var gateways = await registryManager.GetDevicesAsync(9999);
            ViewBag.hub = hub;
            ViewBag.group = group;

            return View(gateways);
        }
    }
}