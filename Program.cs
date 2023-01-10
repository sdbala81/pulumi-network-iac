using System.Collections.Generic;
using Pulumi.AzureNative.ContainerInstance;
using Pulumi.AzureNative.ContainerInstance.Inputs;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Resources;
using Deployment = Pulumi.Deployment;

return await Deployment.RunAsync(() =>
{
    // Create an Azure Resource Group
    var resourceGroup = new ResourceGroup("bala-container-test-group");

    string clientName = "dhl-international";
    var containerGroupName = $"{clientName}-ci";
    
    // Create a container group.
    var containerGroup = new ContainerGroup("my-container-group", new ContainerGroupArgs
    {
        ImageRegistryCredentials = new ImageRegistryCredentialArgs
        {
            Server = "docker.cloudsmith.io",
            Username = "element-data/own_pacages",
            Password = "bY3ZyXppYmzUQ3Hp"
        },
        ResourceGroupName = resourceGroup.Name,
        ContainerGroupName = containerGroupName,
        OsType = "Linux",
        Containers = new[]
        {
            new ContainerArgs
            {
                Image =
                    "docker.cloudsmith.io/element-data/own_pacages/sampledockerwebapplication:latest",
                Name = $"{clientName}containers",
                Ports = new[]
                {
                    new ContainerPortArgs { Port = 80 }
                },
                Resources = new ResourceRequirementsArgs
                {
                    Requests = new ResourceRequestsArgs
                    {
                        Cpu = 1.0,
                        MemoryInGB = 1.5
                    }
                }
            }
        },
        IpAddress = new IpAddressArgs
        {
            Ports = new[]
            {
                new PortArgs
                {
                    Port = 80,
                    Protocol = "TCP"
                }
            },
            Type = "Public"
        },
        RestartPolicy = "always",
    });

    // Create a network interface.
    var networkInterface = new NetworkInterface("my-network-interface", new NetworkInterfaceArgs
    {
        ResourceGroupName = resourceGroup.Name,
        IpConfigurations =
        {
            new NetworkInterfaceIpConfigurationArgs
            {
                Name = "my-ip-config",
                SubnetId = subnet.Id,
                PrivateIpAddressAllocation = "Dynamic",
            },
        },
        ContainerGroup = containerGroup,
    });

    // Create a network security group.
    var networkSecurityGroup = new NetworkSecurityGroup("my-nsg", new NetworkSecurityGroupArgs
    {
        ResourceGroupName = resourceGroup.Name,
        SecurityRules =
        {
            new NetworkSecurityGroupSecurityRuleArgs
            {
                Name = "allow-http",
                Protocol = "Tcp",
                SourceAddressPrefix = "1.2.3.4/32",
                DestinationPortRange = "80",
                Access = "Allow",
                Priority = 100,
                Direction = "Inbound",
            },
        },
    });
    // Add the network interface to the security group.
    var networkSecurityGroupAssociation = new NetworkInterfaceSecurityGroupAssociation("my-nsg-assoc", new NetworkInterfaceSecurityGroupAssociationArgs
    {
        NetworkInterfaceId = networkInterface.Id,
        NetworkSecurityGroupId = networkSecurityGroup.Id,
    });
    
    // Export the primary key of the Storage Account
    return new Dictionary<string, object?>
    {
        ["primaryStorageKey"] = primaryStorageKey
    };
});