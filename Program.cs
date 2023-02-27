return await Deployment.RunAsync(() =>
{
    const string customerName = "lipton-brookbonds";

    // Create an Azure Resource Group
    var resourceGroupName = $"{customerName}-rg".ToLower();
    var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
        {
            ResourceGroupName = resourceGroupName,
            Location = "WestUS2"
        }
    );

    //Create a Vnet
    var containerInstanceVirtualNetwork = $"{customerName}-ci-vnet".ToLower();
    var eLogiqVirtualNetwork = new VirtualNetwork(containerInstanceVirtualNetwork, new VirtualNetworkArgs
    {
        Name = containerInstanceVirtualNetwork,
        ResourceGroupName = resourceGroup.Name,
        Location = "WestUS2",
        AddressSpaces =
        {
            "10.254.0.0/16"
        }
    });

    //Container Instance delegated Public Subnet
    var containerInstanceSubnet = $"{customerName}-ci-subnet".ToLower();
    var PubSub = new Subnet(containerInstanceSubnet, new SubnetArgs
    {
        Name = containerInstanceSubnet,
        ResourceGroupName = resourceGroup.Name,
        VirtualNetworkName = eLogiqVirtualNetwork.Name,
        AddressPrefixes =
        {
            "10.254.1.0/24"
        },
        Delegations = new[]
        {
            new SubnetDelegationArgs
            {
                Name = "delegation",
                ServiceDelegation = new SubnetDelegationServiceDelegationArgs
                {
                    Name = "Microsoft.ContainerInstance/containerGroups",
                    Actions = new[]
                    {
                        "Microsoft.Network/virtualNetworks/subnets/join/action",
                        "Microsoft.Network/virtualNetworks/subnets/prepareNetworkPolicies/action"
                    }
                }
            }
        }
    });

    //Application Gateway  delegated Public Subnet
    var applicationGatewaySubnetName = $"{customerName}-ag-subnet".ToLower();
    _ = new Subnet(applicationGatewaySubnetName, new SubnetArgs
    {
        Name = applicationGatewaySubnetName,
        ResourceGroupName = resourceGroup.Name,
        VirtualNetworkName = eLogiqVirtualNetwork.Name,
        AddressPrefixes =
        {
            "10.254.2.0/24"
        }
    });

    //Network profile for private Container Instance
    var networkProfileName = $"{customerName}-network-profile".ToLower();
    ;
    var networkProfile = new Profile(networkProfileName, new ProfileArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        Name = networkProfileName,
        ContainerNetworkInterface = new ProfileContainerNetworkInterfaceArgs
        {
            Name = "container-network-interface",
            IpConfigurations = new[]
            {
                new ProfileContainerNetworkInterfaceIpConfigurationArgs { Name = "subnet", SubnetId = PubSub.Id }
            }
        }
    });

    //Create the Container Group
    var containerGroupName = $"{customerName}-container-group".ToLower();
    var containerGroup = new ContainerGroup(containerGroupName, new ContainerGroupArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        ContainerGroupName = containerGroupName,
        //Element API CI
        Containers = new[]
        {
            new()
            {
                Image = "docker.cloudsmith.io/element-data/own_pacages/motehus-demo-element-api:1.0.0",
                Name = "dashboard",
                Ports = new[]
                {
                    new ContainerPortArgs { Port = 80 }
                },
                Resources = new ResourceRequirementsArgs
                {
                    Requests = new ResourceRequestsArgs { Cpu = 1, MemoryInGB = 1 }
                }
            },
            //Dashboard Container Instance
            new ContainerArgs
            {
                Image = "docker.cloudsmith.io/element-data/own_pacages/motehus-demo-element-api:1.0.0",
                Name = "cumul-token-provider",
                Ports = new[]
                {
                    new ContainerPortArgs { Port = 3001 }
                },
                Resources = new ResourceRequirementsArgs
                {
                    Requests = new ResourceRequestsArgs { Cpu = 1, MemoryInGB = 1 }
                }
            }
        },

        ImageRegistryCredentials = new[]
        {
            new ImageRegistryCredentialArgs
            {
                Server = "docker.cloudsmith.io", Username = "element-data/own_pacages", Password = "bY3ZyXppYmzUQ3Hp"
            }
        },
        OsType = OperatingSystemTypes.Linux,
        IpAddress = new IpAddressArgs
        {
            Ports = new[]
            {
                new PortArgs
                {
                    Port = 80,
                    Protocol = "TCP"
                },
                new PortArgs
                {
                    Port = 3001,
                    Protocol = "TCP"
                }
            },
            Type = "Private"
        },
        RestartPolicy = "always",
        NetworkProfile = new ContainerGroupNetworkProfileArgs { Id = networkProfile.Id }
    });

    var elementapiCIAddress = containerGroup.IpAddress.Apply(ipAddress => ipAddress?.Ip);

    //Public IP
    var publicIPAddressName = $"{customerName}-public-ip";

    var publicIp = new PublicIp(publicIPAddressName, new PublicIpArgs
    {
        Name = publicIPAddressName,
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        AllocationMethod = "Static",
        Sku = "Standard",
        SkuTier = "Regional"
    });

    //Web application Policy
    var webApplicationFirewallPolicyName = $"{customerName}-firewall-policy";

    var webApplicationFirewallPolicy = new WebApplicationFirewallPolicy(webApplicationFirewallPolicyName,
        new WebApplicationFirewallPolicyArgs
        {
            PolicySettings = new PolicySettingsArgs
            {
                RequestBodyCheck = true,
                MaxRequestBodySizeInKb = 128,
                FileUploadLimitInMb = 100,
                State = "Enabled",
                Mode = "Prevention"
            },
            CustomRules = new[]
            {
                new WebApplicationFirewallCustomRuleArgs
                {
                    Action = "Block",
                    MatchConditions = new[]
                    {
                        new MatchConditionArgs
                        {
                            MatchValues = new[]
                            {
                                "175.157.40.245"
                            },
                            MatchVariables = new[]
                            {
                                new MatchVariableArgs
                                {
                                    VariableName = "RemoteAddr"
                                }
                            },
                            Operator = "IPMatch",
                            NegationConditon = true
                        }
                    },
                    Name = "Rule2",
                    Priority = 1,
                    RuleType = "MatchRule"
                }
            },
            Location = resourceGroup.Location,
            ManagedRules = new ManagedRulesDefinitionArgs
            {
                ManagedRuleSets = new[]
                {
                    new ManagedRuleSetArgs
                    {
                        RuleSetType = "OWASP",
                        RuleSetVersion = "3.0"
                    }
                }
            },
            PolicyName = webApplicationFirewallPolicyName,
            ResourceGroupName = resourceGroup.Name
        });

    //Application Gateway
    var applicationGatewayName = $"{customerName}-application-gateway";

    _ = new ApplicationGateway(applicationGatewayName, new ApplicationGatewayArgs
        {
            ApplicationGatewayName = applicationGatewayName,
            BackendAddressPools = new[]
            {
                new ApplicationGatewayBackendAddressPoolArgs
                {
                    BackendAddresses = new[]
                    {
                        //Element API IP
                        containerGroup.IpAddress.Apply(ipAddress => new ApplicationGatewayBackendAddressArgs
                        {
                            IpAddress = "10.254.1.4"
                            //"10.254.1.4" 
                            //PubSub.AddressPrefix,
                        })
                    },
                    Name = "appgwpool"
                },
                new ApplicationGatewayBackendAddressPoolArgs
                {
                    BackendAddresses = new[]
                    {
                        //Dashboard IP
                        containerGroup.IpAddress.Apply(ipAddress => new ApplicationGatewayBackendAddressArgs
                            {
                                IpAddress = "172.16.0.4"
                                //"10.254.1.4" 
                                //PubSub.AddressPrefix,
                            }
                        )
                    },
                    Name = "secondappgwpool"
                }
            },
            BackendHttpSettingsCollection = new[]
            {
                new ApplicationGatewayBackendHttpSettingsArgs
                {
                    CookieBasedAffinity = "Disabled",
                    Name = "appgwbhs",
                    Port = 80,
                    Protocol = "Http",
                    RequestTimeout = 30
                },
                new ApplicationGatewayBackendHttpSettingsArgs
                {
                    CookieBasedAffinity = "Disabled",
                    Name = "appgsecond",
                    Port = 3001,
                    Protocol = "Http",
                    RequestTimeout = 30
                }
            },

            FrontendIPConfigurations = new[]
            {
                new ApplicationGatewayFrontendIPConfigurationArgs
                {
                    Name = "frontendIp",
                    PublicIPAddress = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{publicIPAddressName}"
                    }
                }
            },
            FrontendPorts = new[]
            {
                new ApplicationGatewayFrontendPortArgs
                {
                    Name = "appgwfp80",
                    Port = 80
                },
                new ApplicationGatewayFrontendPortArgs
                {
                    Name = "secondport",
                    Port = 3001
                }
            },
            GatewayIPConfigurations = new[]
            {
                new ApplicationGatewayIPConfigurationArgs
                {
                    Name = "appgwipc",
                    Subnet = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/civnet-eLogiq/subnets/agsubnet"
                    }
                }
            },
            HttpListeners = new[]
            {
                new ApplicationGatewayHttpListenerArgs
                {
                    FrontendIPConfiguration = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/frontendIPConfigurations/frontendIp"
                    },
                    FrontendPort = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/frontendPorts/appgwfp80"
                    },
                    Name = "appgwhttplistener",
                    Protocol = "Http"
                },
                //
                new ApplicationGatewayHttpListenerArgs
                {
                    FrontendIPConfiguration = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/frontendIPConfigurations/frontendIp"
                    },
                    FrontendPort = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/frontendPorts/secondport"
                    },
                    Name = "secondappgwhttplistener",
                    Protocol = "Http"
                }
            },
            Location = resourceGroup.Location,
            RequestRoutingRules = new[]
            {
                new ApplicationGatewayRequestRoutingRuleArgs
                {
                    BackendAddressPool = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/backendAddressPools/appgwpool"
                    },
                    BackendHttpSettings = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/backendHttpSettingsCollection/appgwbhs"
                    },
                    HttpListener = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/httpListeners/appgwhttplistener"
                    },
                    Name = "appgwrule",
                    Priority = 1,
                    // UrlPathMap = new AzureNative.Network.Inputs.SubResourceArgs
                    // {
                    //     Id = $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/appgw/urlPathMaps/pathMap1",
                    // },

                    RuleType = "Basic"
                },

                new ApplicationGatewayRequestRoutingRuleArgs
                {
                    BackendAddressPool = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/backendAddressPools/secondappgwpool"
                    },
                    BackendHttpSettings = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/backendHttpSettingsCollection/appgsecond"
                    },
                    HttpListener = new SubResourceArgs
                    {
                        Id =
                            $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/applicationGateways/{applicationGatewayName}/httpListeners/secondappgwhttplistener"
                    },
                    Name = "secondappgwrule",
                    Priority = 2,
                    

                    RuleType = "Basic"
                }
            },
            FirewallPolicy = new SubResourceArgs
            {
                Id =
                    $"/subscriptions/2a99b930-f4ca-4062-8dca-5bafcbe540db/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/ApplicationGatewayWebApplicationFirewallPolicies/{webApplicationFirewallPolicyName}"
            },
            ResourceGroupName = resourceGroup.Name,
            Sku = new ApplicationGatewaySkuArgs
            {
                Capacity = 2,
                //Name = "Standard_v2",
                //Tier = "Standard_v2",
                Name = "WAF_v2",
                Tier = "WAF_v2"
            }
        },
        new CustomResourceOptions
        {
            DependsOn = { publicIp, webApplicationFirewallPolicy }
        }
    );
});