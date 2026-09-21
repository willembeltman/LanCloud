using LanCloud.Host;
using LanCloud.Shared.Models;

var config = new LanCloudHostConfig(
    "https://localhost:7087", 
    "wss://127.0.0.1:7087",
    [new LocalShare("E:\\Films")]);

HostProgram.Run(args, config);
