using DoubleSharp.Pretty;
using QuimeraCore;

var ipsw = new IpswLoader(args[0]);
var deviceTree = ipsw.DeviceTree;
var kernelCache = ipsw.KernelCache;
//File.WriteAllBytes("devicetree.bin", deviceTree);
var parsedDeviceTree = DeviceTree.Parse(deviceTree);
DeviceTree.Dump(parsedDeviceTree);
var macho = new MachOLoader(kernelCache);