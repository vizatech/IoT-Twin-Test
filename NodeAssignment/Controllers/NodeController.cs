using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using NodeAssignment.Models;

using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace NodeAssignment.Controllers
{
    public class NodeController : Controller
    {
        DeviceManager _dm;

        public NodeController()
        {
            _dm = new DeviceManager();
        }

        public async Task<ActionResult> Index(string gateway, string hub, string group)
        {
            // List the reported properties of the gateway
            var registryManager = _dm.registryMangerForHub(hub, group);
            var twin = await registryManager.GetTwinAsync(gateway);
            var reported = twin.Properties.Reported.ToJson();
            var reportedDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(reported);

            var nodes = new Dictionary<int, ReportedNode>();
            for (var i = 0; i < 16; i++)
            {
                var nodeKey = string.Format("node{0}", i.ToString("D2"));
                var nodeJson = reportedDict[nodeKey];
                ReportedNode reportedNode = JsonConvert.DeserializeObject<ReportedNode>(nodeJson.ToString());
                nodes[i] = reportedNode;
            }
            
            ViewBag.gateway = gateway;
            ViewBag.hub = hub;
            ViewBag.group = group;

            return View(nodes);
        }
        public async Task<ActionResult> Move(string sourceGateway, string sourceHub, string sourceGroup, int sourceSlot)
        {
            var resourceGroups = await _dm.azureClient().ResourceGroups.ListAsync();

            List<MoveViewModels> MoveModels = new List<MoveViewModels>();
                                                            

            foreach (var group in resourceGroups)
            {
                var hubs = await _dm.rmClient().ResourceGroups.ListResourcesAsync(group.Name, "resourceType eq 'Microsoft.Devices/IotHubs'");
                foreach (var hub in hubs)
                {
                    MoveViewModels MoveModel = new MoveViewModels();
                    MoveModel.hub = hub.Name;
                    MoveModel.group = group.Name;

                    var registryManager = _dm.registryMangerForHub(hub.Name, group.Name);
                    var gateways = await registryManager.GetDevicesAsync(9999);

                    MoveModel.gateways = new List<Gateway>();

                    foreach (var gateway in gateways)
                    {
                        Gateway HubGateway = new Gateway();
                        HubGateway.Id = gateway.Id;
                        HubGateway.ConnectionState = gateway.ConnectionState.ToString();
                        HubGateway.ConnectionStateUpdatedTime = gateway.ConnectionStateUpdatedTime.ToString();

                        var twin = await registryManager.GetTwinAsync(gateway.Id);
                        var desired = twin.Properties.Desired.ToJson();
                        var desiredDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(desired);

                        var nodes = new Dictionary<int, DesiredNode>();
                        for (var i = 0; i < 16; i++)
                        {
                            var nodeKey = string.Format("node_settings_{0}", i.ToString("D2"));
                            var nodeJson = desiredDict[nodeKey];
                            DesiredNode desiredNode = JsonConvert.DeserializeObject<DesiredNode>(nodeJson.ToString());
                            nodes[i] = desiredNode;
                        }

                        HubGateway.Nodes = nodes;

                        MoveModel.gateways.Add(HubGateway);
                    }
                    MoveModels.Add(MoveModel);
                }
            }
            ViewBag.gateway = sourceGateway;
            ViewBag.hub = sourceHub;
            ViewBag.group = sourceGroup;

            ViewBag.slot = sourceSlot;

            // move the sensor node from slot <slot> to another gateway and slot of choice

            // hints:
            // - modify desired properties of the gateways device twin
            // - use `UpdateTwin` below to update a specific part (e.g. a desired node property) of a device twin
            return View(MoveModels);
        }

        public async Task<ActionResult> ShowDesired(string gateway, string hub, string group, int slot)
        {
            DesiredNode desiredSourceNode = await getNode(gateway, hub, group, slot);

            ViewBag.gateway = gateway;
            ViewBag.hub = hub;
            ViewBag.group = group;

            ViewBag.slot = slot;

            // move the sensor node from slot <slot> to another gateway and slot of choice

            // hints:
            // - modify desired properties of the gateways device twin
            // - use `UpdateTwin` below to update a specific part (e.g. a desired node property) of a device twin
            return View(desiredSourceNode);
        }

        public async Task<DesiredNode> getNode(string gateway, string hub, string group, int slot)
        {
            var registryManager = _dm.registryMangerForHub(hub, group);
            var twin = await registryManager.GetTwinAsync(gateway);
            var desired = twin.Properties.Desired.ToJson();
            var desiredDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(desired);

            var nodeKey = string.Format("node_settings_{0}", slot.ToString("D2"));
            var nodeJson = desiredDict[nodeKey];

            DesiredNode desiredNode = JsonConvert.DeserializeObject<DesiredNode>(nodeJson.ToString());

            return desiredNode;
        }

        public async Task<ActionResult> CheckForUpdateTwin(string SourceGateway, string SourceHub, string SourceGroup, int SourceSlot, string TargetGateway, string TargetHub, string TargetGroup, int TargetSlot)
        {                                  
            DesiredNode desiredSourceNode = await getNode(SourceGateway, SourceHub, SourceGroup, SourceSlot);

            DesiredNode targetSourceNode = await getNode(TargetGateway, TargetHub, TargetGroup, TargetSlot);

            DesiredNode desiredUpdatedNode = new DesiredNode();
            if ((targetSourceNode.device_id == "0") || (targetSourceNode.device_id == ""))
            {
                await UpdateTwin(TargetGateway, TargetHub, TargetGroup, TargetSlot, desiredSourceNode);

                desiredUpdatedNode = await getNode(TargetGateway, TargetHub, TargetGroup, TargetSlot);

                if (desiredUpdatedNode.device_id == desiredSourceNode.device_id) await UpdateTwin(SourceGateway, SourceHub, SourceGroup, SourceSlot, new DesiredNode());
            }
            ViewBag.gateway = TargetGateway;
            ViewBag.hub = TargetHub;
            ViewBag.group = TargetGroup;
            ViewBag.slot = TargetSlot;

            return View("~/Views/Node/ShowDesired.cshtml", desiredUpdatedNode);
        }

        private async Task UpdateTwin(string gateway, string hub, string group, int slot, DesiredNode node)
        {
            // this function updates the desired properties of the gateway,
            // and replaces the sensor node in at position <slot> with
            // the new sensor node information in contained in <node>.

            var registryManager = _dm.registryMangerForHub(hub, group);
            var twin = await registryManager.GetTwinAsync(gateway);
            var desired = twin.Properties.Desired.ToJson();

            var desiredObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(desired);
            var slotKey = string.Format("node_settings_{0}", slot.ToString("D2"));

            //var bck = JsonConvert.DeserializeObject<DesiredNode>(desiredObj[slotKey].ToString());

            // patch device twin
            var patch = string.Format("{{properties : {{ desired : {{ {0} : {1} }} }} }}", slotKey, JsonConvert.SerializeObject(node));
            twin = await registryManager.UpdateTwinAsync(gateway, patch, twin.ETag);
        }

    }
}