namespace Microsoft.Azure.PowerShell.Cmdlets.VMWare.Models.Api20200320
{
    using static Microsoft.Azure.PowerShell.Cmdlets.VMWare.Runtime.Extensions;

    /// <summary>Resource tags.</summary>
    public partial class PrivateCloudUpdateTags :
        Microsoft.Azure.PowerShell.Cmdlets.VMWare.Models.Api20200320.IPrivateCloudUpdateTags,
        Microsoft.Azure.PowerShell.Cmdlets.VMWare.Models.Api20200320.IPrivateCloudUpdateTagsInternal
    {

        /// <summary>Creates an new <see cref="PrivateCloudUpdateTags" /> instance.</summary>
        public PrivateCloudUpdateTags()
        {

        }
    }
    /// Resource tags.
    public partial interface IPrivateCloudUpdateTags :
        Microsoft.Azure.PowerShell.Cmdlets.VMWare.Runtime.IJsonSerializable,
        Microsoft.Azure.PowerShell.Cmdlets.VMWare.Runtime.IAssociativeArray<string>
    {

    }
    /// Resource tags.
    internal partial interface IPrivateCloudUpdateTagsInternal

    {

    }
}