using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn.Authorization.ABAC.Xacml
{
    public class XacmlPolicyDelegation
    {
        /// <summary>
        /// The delegation Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Who is the resource owner
        /// </summary>
        public string DelegatedFrom { get; set; }

        /// <summary>
        /// Who it is delegated from
        /// </summary>
        public string DelegatedTo { get; set; }

        /// <summary>
        /// Who did the delegation
        /// </summary>
        public string DelegatedBy { get; set; }

        /// <summary>
        /// Who long is the delegation 
        /// </summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// The time the delegation is no longer valid
        /// </summary>
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Identification of the resource related to the policy. Used to
        /// filter the delegated polices. Who this will be handled is up to the application
        /// where the PDP is used and the implemented policy retrieval point
        /// </summary>
        public string PolicyResource { get; set; }

        /// <summary>
        /// The path to the delegated policy containing the XACML Policy containg all delegated rights.
        /// </summary>
        public string PolicyLocation { get; set; }
    }
}
