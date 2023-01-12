using System.Collections.Generic;
using Pulumi;
using Pulumi.Azure.ContainerService;
using Pulumi.Azure.ContainerService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Network;
using Pulumi.Azure.Network.Inputs;

return await Deployment.RunAsync(() =>
{
    // Create a resource group.
    var resourceGroup = new ResourceGroup("lipton-brokebonds", new ResourceGroupArgs
    {
        Name = "lipton-brokebonds",
        Location = "westeurope"
    });
    // Create a virtual network.
    var network = new VirtualNetwork("lipton-brokebonds-network", new VirtualNetworkArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        AddressSpaces = { "10.0.0.0/16" }
    });
    // Create a subnet
    var subnet = new Subnet("lipton-brokebonds-subnet", new SubnetArgs
    {
        ResourceGroupName = resourceGroup.Name,
        VirtualNetworkName = network.Name,
        AddressPrefixes = new[]
        {
            "10.0.2.0/24"
        }
    });
    // Create a container group.
    var containerGroup = new Group("lipton-brokebonds-container-group", new GroupArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        OsType = "Linux",
        IpAddressType = "Private",
        SubnetIds = subnet.Id,
        Containers = new[]
        {
            new GroupContainerArgs
            {
                Name = "lipton-brokebonds-container",
                Image = "docker.cloudsmith.io/element-data/own_pacages/sampledockerwebapplication:latest",
                Cpu = 0.5,
                Memory = 0.5,
                Ports = new[]
                {
                    new GroupContainerPortArgs
                    {
                        Port = 80
                    }
                }
            }
        },
        ImageRegistryCredentials = new[]
        {
            new GroupImageRegistryCredentialArgs
            {
                Server = "docker.cloudsmith.io",
                Username = "element-data/own_pacages",
                Password = "bY3ZyXppYmzUQ3Hp"
            }
        }
    });
    // Create a network security group and a rule to allow traffic from a specific IP.
    var networkSecurityGroup = new NetworkSecurityGroup("lipton-brokebonds-nsg", new NetworkSecurityGroupArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        SecurityRules =
        {
            new NetworkSecurityGroupSecurityRuleArgs
            {
                Name = "allow-http",
                Protocol = "Tcp",
                SourceAddressPrefix = "123.231.110.255/32",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "80",
                Access = "Allow",
                Priority = 100,
                Direction = "Inbound",
                SourcePortRange = "80"
            }
        }
    });
    // Create a network interface.
    var networkInterface = new NetworkInterface("lipton-brokebonds-network-interface", new NetworkInterfaceArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        IpConfigurations = new[]
        {
            new NetworkInterfaceIpConfigurationArgs
            {
                Name = "internal",
                SubnetId = subnet.Id,
                PrivateIpAddressAllocation = "Dynamic"
            }
        }
    });
    // Add the network interface to the security group.
    var networkSecurityGroupAssociation = new NetworkInterfaceSecurityGroupAssociation("lipton-brokebonds-nsg-assoc",
        new NetworkInterfaceSecurityGroupAssociationArgs
        {
            NetworkInterfaceId = networkInterface.Id,
            NetworkSecurityGroupId = networkSecurityGroup.Id
        });
    // Export the primary key of the Storage Account
    return new Dictionary<string, object?>
    {
        ["container_ip"] = containerGroup.IpAddress
    };
});