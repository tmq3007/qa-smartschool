using System.Collections.Generic;

namespace QASmartClass.Models
{
    public class TopologyNode
    {
        public string MachineId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string NodeType { get; set; } = "Student"; // "Teacher" or "Student"
        public string Status { get; set; } = "Offline"; // "Online", "Offline", "Warning"
        public double X { get; set; }
        public double Y { get; set; }
        
        // Transient property for UI updating
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSelected { get; set; }
    }

    public class TopologyData
    {
        public string RoomId { get; set; } = "";
        public List<TopologyNode> Nodes { get; set; } = new List<TopologyNode>();
    }
}
