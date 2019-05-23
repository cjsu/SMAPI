using System.Reflection;

namespace StardewModdingAPI.Framework.Reflection
{
    /// <summary>A cached member reflection result.</summary>
    internal readonly struct CacheEntry
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether the lookup found a valid match.</summary>
        public bool IsValid { get; }

        /// <summary>The reflection data for this member (or <c>null</c> if invalid).</summary>
        public MemberInfo MemberInfo { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="isValid">Whether the lookup found a valid match.</param>
        /// <param name="memberInfo">The reflection data for this member (or <c>null</c> if invalid).</param>
        public CacheEntry(bool isValid, MemberInfo memberInfo)
        {
            this.IsValid = isValid;
            this.MemberInfo = memberInfo;
        }
    }
}
