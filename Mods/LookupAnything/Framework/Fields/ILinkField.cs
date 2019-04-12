using Pathoschild.Stardew.LookupAnything.Framework.Subjects;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A field which links to another entry.</summary>
    internal interface ILinkField : ICustomField
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get the subject the link points to.</summary>
        ISubject GetLinkSubject();
    }
}
