﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Management.Automation;
using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
using Microsoft.Azure.Commands.SignalR.Models;
using Microsoft.Azure.Commands.SignalR.Properties;
using Microsoft.Azure.Management.SignalR;
using Microsoft.Azure.Management.SignalR.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.Commands.SignalR.Cmdlets
{
    [Cmdlet("Update", ResourceManager.Common.AzureRMConstants.AzureRMPrefix + "SignalR", SupportsShouldProcess = true, DefaultParameterSetName = ResourceGroupParameterSet)]
    [OutputType(typeof(PSSignalRResource))]
    public class UpdateAzureRmSignalR : SignalRCmdletBase, IWithInputObject, IWithResourceId
    {
        private const string DefaultSku = "Standard_S1";
        private const int DefaultUnitCount = 1;

        [Parameter(
            Mandatory = false,
            ParameterSetName = ResourceGroupParameterSet,
            HelpMessage = "The resource group name. The default one will be used if not specified.")]
        [ResourceGroupCompleter()]
        [ValidateNotNullOrEmpty()]
        public override string ResourceGroupName { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = ResourceGroupParameterSet,
            HelpMessage = "The SignalR service name.")]
        [ValidateNotNullOrEmpty()]
        public string Name { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = ResourceIdParameterSet,
            ValueFromPipeline = true,
            HelpMessage = "The SignalR service resource ID.")]
        [ValidateNotNullOrEmpty]
        public string ResourceId { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = InputObjectParameterSet,
            ValueFromPipeline = true,
            HelpMessage = "The SignalR resource object.")]
        [ValidateNotNull]
        public PSSignalRResource InputObject { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The SignalR service SKU. Default to \"Standard_S1\".")]
        [PSArgumentCompleter("Free_F1", "Standard_S1")]
        public string Sku { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The SignalR service unit count, value only from {1, 2, 5, 10, 20, 50, 100}. Default to 1.")]
        [PSArgumentCompleter("1", "2", "5", "10", "20", "50", "100")]
        public int? UnitCount { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The tags for the SignalR service.")]
        public IDictionary<string, string> Tag { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The features for the SignalR service.")]
        public IList<SignalRFeature> Feature { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The Cors for the SignalR service.")]
        public SignalRCorsSettings Cors { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Run the cmdlet in background job.")]
        public SwitchParameter AsJob { get; set; }

        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();

            RunCmdlet(() =>
            {
                switch (ParameterSetName)
                {
                    case ResourceGroupParameterSet:
                        ResolveResourceGroupName();
                        break;
                    case ResourceIdParameterSet:
                        this.LoadFromResourceId();
                        break;
                    case InputObjectParameterSet:
                        this.LoadFromInputObject();
                        break;
                    default:
                        throw new ArgumentException(Resources.ParameterSetError);
                }

                if (ShouldProcess($"SignalR service {ResourceGroupName}/{Name}", "update"))
                {
                    PromptParameter(nameof(ResourceGroupName), ResourceGroupName);
                    PromptParameter(nameof(Name), Name);
                    PromptParameter(nameof(Sku), Sku, true, DefaultSku);
                    PromptParameter(nameof(UnitCount), UnitCount, true, DefaultUnitCount);
                    PromptParameter(nameof(Tag), Tag == null ? null : JsonConvert.SerializeObject(Tag));
                    PromptParameter(nameof(Feature), Feature == null ? null : JsonConvert.SerializeObject(Feature));
                    PromptParameter(nameof(Cors), Cors == null ? null : JsonConvert.SerializeObject(Cors));

                    Sku = Sku ?? DefaultSku;
                    UnitCount = UnitCount ?? DefaultUnitCount;

                    var parameters = new SignalRUpdateParameters(
                        tags: Tag,
                        sku: new ResourceSku(name: Sku, capacity: UnitCount),
                        properties: new SignalRCreateOrUpdateProperties(features: Feature, cors: Cors));

                    Client.SignalR.Update(ResourceGroupName, Name, parameters);

                    var signalr = (Client.SignalR.Get(ResourceGroupName, Name));
                    WriteObject(new PSSignalRResource(signalr));
                }
            });
        }
    }
}
