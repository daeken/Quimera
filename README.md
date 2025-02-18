!Before Using!
==============

Prior to using Quimera, you need to resign the main dotnet binary to have the hypervisor entitlement. From the directory into which you checked out the repo (or any containing the plist from the repo):

```
sudo codesign -f -s - --entitlements dotnet-entitlements.plist `which dotnet`
```
