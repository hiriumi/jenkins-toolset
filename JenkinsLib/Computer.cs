using System.Collections.Generic;
using ByteSizeLib;
using Newtonsoft.Json;

namespace JenkinsLib
{
    public class Action
    {
    }

    public class Executor
    {
    }

    public class LoadStatistics
    {
        public string _class { get; set; }
    }

    public class HudsonNodeMonitorsSwapSpaceMonitor
    {
        public string _class { get; set; }
        public double availablePhysicalMemory { get; set; }
        public double availableSwapSpace { get; set; }
        public double totalPhysicalMemory { get; set; }
        public double totalSwapSpace { get; set; }
    }

    public class HudsonNodeMonitorsTemporarySpaceMonitor
    {
        public string _class { get; set; }
        public double timestamp { get; set; }
        public string path { get; set; }
        public double size { get; set; }
    }

    public class HudsonNodeMonitorsDiskSpaceMonitor
    {
        public string _class { get; set; }
        public double timestamp { get; set; }
        public string path { get; set; }
        public double size { get; set; }
    }

    public class HudsonNodeMonitorsResponseTimeMonitor
    {
        public string _class { get; set; }
        public double timestamp { get; set; }
        public int average { get; set; }
    }

    public class HudsonNodeMonitorsClockMonitor
    {
        public string _class { get; set; }
        public int diff { get; set; }
    }

    public class MonitorData
    {
        public string _class { get; set; }

        [JsonProperty(PropertyName = "hudson.node_monitors.SwapSpaceMonitor")]
        public HudsonNodeMonitorsSwapSpaceMonitor SwapSpaceMonitor { get; set; }

        [JsonProperty(PropertyName = "hudson.node_monitors.TemporarySpaceMonitor")]
        public HudsonNodeMonitorsTemporarySpaceMonitor TemporarySpaceMonitor { get; set; }

        [JsonProperty(PropertyName = "hudson.node_monitors.DiskSpaceMonitor")]
        public HudsonNodeMonitorsDiskSpaceMonitor DiskSpaceMonitor { get; set; }

        [JsonProperty(PropertyName = "hudson.node_monitors.ArchitectureMonitor")]
        public string ArchitectureMonitor { get; set; }

        [JsonProperty(PropertyName = "hudson.node_monitors.ResponseTimeMonitor")]
        public HudsonNodeMonitorsResponseTimeMonitor ResponseTimeMonitor { get; set; }

        [JsonProperty(PropertyName = "hudson.node_monitors.ClockMonitor")]
        public HudsonNodeMonitorsClockMonitor ClockMonitor { get; set; }
    }

    public class OfflineCause
    {
    }

    public class Computer
    {
        public string _class { get; set; }
        public IList<Action> actions { get; set; }
        public string displayName { get; set; }
        public IList<Executor> executors { get; set; }
        public string icon { get; set; }
        public string iconClassName { get; set; }
        public bool idle { get; set; }
        public bool jnlpAgent { get; set; }
        public bool launchSupported { get; set; }
        public LoadStatistics loadStatistics { get; set; }
        public bool manualLaunchAllowed { get; set; }

        [JsonProperty(PropertyName = "monitorData")]
        public MonitorData monitorData { get; set; }
        public int numExecutors { get; set; }
        public bool offline { get; set; }
        public OfflineCause offlineCause { get; set; }
        public string offlineCauseReason { get; set; }
        public IList<object> oneOffExecutors { get; set; }
        public bool temporarilyOffline { get; set; }
        public string ImageFileName
        {
            get
            {
                if (monitorData.ArchitectureMonitor == null)
                    return string.Empty;

                if (monitorData.ArchitectureMonitor.Contains("Windows"))
                    return "../Images/windows.png";

                if (monitorData.ArchitectureMonitor.Contains("Linux"))
                    return "../Images/linux.png";

                if (monitorData.ArchitectureMonitor.Contains("Apple"))
                    return "../Images/apple.png";

                return string.Empty;
            }
        }

        public string DisplayPhysicalMemorySize
        {
            get
            {
                if (monitorData.SwapSpaceMonitor != null)
                    return ByteSize.FromBytes(monitorData.SwapSpaceMonitor.totalPhysicalMemory).ToString("GB");
                
                return ByteSize.FromBytes(0).ToString("GB");
            } 
        }

        public string DisplayFreeMemorySize
        {
            get
            {
                if (monitorData.SwapSpaceMonitor != null)
                    return ByteSize.FromBytes(monitorData.SwapSpaceMonitor.availablePhysicalMemory).ToString("GB");

                return ByteSize.FromBytes(0).ToString("GB");
            }
        }

        public string DisplayFreeDiskSize
        {
            get
            {
                if (monitorData.SwapSpaceMonitor != null)
                    return ByteSize.FromBytes(monitorData.DiskSpaceMonitor.size).ToString("GB");

                return ByteSize.FromBytes(0).ToString("GB");
            }
        }


        public string FreeDiskSpace
        {
            get
            {
                if (monitorData.SwapSpaceMonitor != null)
                    return ByteSize.FromBytes(monitorData.SwapSpaceMonitor.availablePhysicalMemory).ToString("GB");

                return ByteSize.FromBytes(0).ToString("GB");
            }
        }

        public ComputerType ComputerType
        {
            get
            {
                var returnType = ComputerType.Unknown;
                if (monitorData.ArchitectureMonitor == null)
                    return returnType;

                if (monitorData.ArchitectureMonitor.Contains("Windows"))
                    returnType = ComputerType.Windows;

                if (monitorData.ArchitectureMonitor.Contains("Linux"))
                    returnType = ComputerType.Linux;
                
                if (monitorData.ArchitectureMonitor.Contains("Mac"))
                    returnType = ComputerType.Mac;

                return returnType;

            }
        }
    }

    public class Example
    {
        public string _class { get; set; }
        public int busyExecutors { get; set; }
        public List<Computer> computer { get; set; }
        public string displayName { get; set; }
        public int totalExecutors { get; set; }
    }


}