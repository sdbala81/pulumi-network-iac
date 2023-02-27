// Global using directives

global using Pulumi;
global using Pulumi.Azure.Network;
global using Pulumi.Azure.Network.Inputs;
global using Pulumi.AzureNative.ContainerInstance;
global using Pulumi.AzureNative.ContainerInstance.Inputs;
global using Pulumi.AzureNative.Network;
global using Pulumi.AzureNative.Network.Inputs;
global using Pulumi.AzureNative.Resources;
global using ApplicationGateway = Pulumi.AzureNative.Network.ApplicationGateway;
global using ApplicationGatewayArgs = Pulumi.AzureNative.Network.ApplicationGatewayArgs;
global using ApplicationGatewayBackendAddressPoolArgs =
    Pulumi.AzureNative.Network.Inputs.ApplicationGatewayBackendAddressPoolArgs;
global using ApplicationGatewayFrontendPortArgs = Pulumi.AzureNative.Network.Inputs.ApplicationGatewayFrontendPortArgs;
global using ApplicationGatewayHttpListenerArgs = Pulumi.AzureNative.Network.Inputs.ApplicationGatewayHttpListenerArgs;
global using ApplicationGatewayRequestRoutingRuleArgs =
    Pulumi.AzureNative.Network.Inputs.ApplicationGatewayRequestRoutingRuleArgs;
global using ApplicationGatewaySkuArgs = Pulumi.AzureNative.Network.Inputs.ApplicationGatewaySkuArgs;
global using AzureE = Pulumi.Azure;
global using Deployment = Pulumi.Deployment;
global using Profile = Pulumi.Azure.Network.Profile;
global using ProfileArgs = Pulumi.Azure.Network.ProfileArgs;
global using Subnet = Pulumi.Azure.Network.Subnet;
global using SubnetArgs = Pulumi.Azure.Network.SubnetArgs;
global using VirtualNetwork = Pulumi.Azure.Network.VirtualNetwork;
global using VirtualNetworkArgs = Pulumi.Azure.Network.VirtualNetworkArgs;