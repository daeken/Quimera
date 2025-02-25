using QuimeraCore;

var ipsw = new IpswLoader(args[0]);
var kernelCache = ipsw.KernelCache;
File.WriteAllBytes("kernelcache.bin", kernelCache);