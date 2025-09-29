# Intranet Penetration Example

## Example Description

This example demonstrates an intranet penetration setup with the following flow:

```mermaid
flowchart TD;
    subgraph Internet
    Browser1--*.demo.com-->PublicServer{Public Server};
    Browser2--*.demo.com-->PublicServer;
    end

    subgraph Intranet1
    PublicServer<--siteA1.demo.com-->IntranetSite1;
    PublicServer<--siteA2.demo.com-->IntranetSite2;
    end

    subgraph Intranet2
    PublicServer<--baidu.demo, qq.demo.com-->IntranetProxy1{Intranet Proxy 1};

    IntranetProxy1<--baidu.demo.com-->Forward to baidu.com;
    IntranetProxy1<--qq.demo.com-->Forward to qq.com
    end

```

## Description

- Cyarp.Sample.PublicReverseProxy is a reverse proxy service deployed on an internet server, responsible for forwarding requests to intranet websites.
- Cyarp.Sample.IntranetSite1, Cyarp.Sample.IntranetSite2 are websites running on company intranet servers, which can also run in containers as long as they can access the public server.
- Cyarp.Sample.IntranetProxy is a proxy service running on company intranet servers, responsible for forwarding requests to other intranet websites.

Point your domain to the public server, for example resolve the wildcard domain \*.demo.com to the server where PublicReverseProxy is located.  
Configure ConnectHeaders:HOST in Appsettings.json for IntranetSite1 and IntranetSite2 as siteA1.demo.com and siteA2.demo.com respectively.

## Development Environment Testing

Start all projects in the aspnet-sites-tunnel.sln solution simultaneously, and test access through test.http in this directory.  
If you want to test from a browser, add siteA1.demo.com and siteA2.demo.com pointing to 127.0.0.1 in your computer's hosts file.  
Then you can access http://siteA1.demo.com:5080 and http://siteA2.demo.com:5080 from the browser.  
5080 is the port for PublicReverseProxy.

Start both Cyarp.Sample.PublicReverseProxy and Cyarp.Sample.IntranetClient simultaneously, and select qq.demo.com and baidu.demo.com in the IntranetClient window,
