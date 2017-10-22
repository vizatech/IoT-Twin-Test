using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace NodeAssignment.Models
{
    public class MoveViewModels
    {
        public string hub { get; set; }
        public string group { get; set; }
        public List<Gateway> gateways { get; set; }
    }
    public class Gateway
    {
        public string Id { get; set; }
        public string ConnectionState { get; set; }
        public string ConnectionStateUpdatedTime { get; set; }
        public Dictionary<int, DesiredNode> Nodes { get; set; } 
    }
}