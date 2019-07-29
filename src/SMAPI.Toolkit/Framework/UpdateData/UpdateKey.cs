using System;

namespace StardewModdingAPI.Toolkit.Framework.UpdateData
{
    /// <summary>A namespaced mod ID which uniquely identifies a mod within a mod repository.</summary>
    public class UpdateKey : IEquatable<UpdateKey>
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The raw update key text.</summary>
        public string RawText { get; }

        /// <summary>The mod repository containing the mod.</summary>
        public ModRepositoryKey Repository { get; }

        /// <summary>The mod ID within the repository.</summary>
        public string ID { get; }

        /// <summary>Whether the update key seems to be valid.</summary>
        public bool LooksValid { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="rawText">The raw update key text.</param>
        /// <param name="repository">The mod repository containing the mod.</param>
        /// <param name="id">The mod ID within the repository.</param>
        public UpdateKey(string rawText, ModRepositoryKey repository, string id)
        {
            this.RawText = rawText;
            this.Repository = repository;
            this.ID = id;
            this.LooksValid =
                repository != ModRepositoryKey.Unknown
                && !string.IsNullOrWhiteSpace(id);
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="repository">The mod repository containing the mod.</param>
        /// <param name="id">The mod ID within the repository.</param>
        public UpdateKey(ModRepositoryKey repository, string id)
            : this($"{repository}:{id}", repository, id) { }

        /// <summary>Parse a raw update key.</summary>
        /// <param name="raw">The raw update key to parse.</param>
        public static UpdateKey Parse(string raw)
        {
            // split parts
            string[] parts = raw?.Split(':');
            if (parts == null || parts.Length != 2)
                return new UpdateKey(raw, ModRepositoryKey.Unknown, null);

            // extract parts
            string repositoryKey = parts[0].Trim();
            string id = parts[1].Trim();
            if (string.IsNullOrWhiteSpace(id))
                id = null;

            // parse
            if (!Enum.TryParse(repositoryKey, true, out ModRepositoryKey repository))
                return new UpdateKey(raw, ModRepositoryKey.Unknown, id);
            if (id == null)
                return new UpdateKey(raw, repository, null);

            return new UpdateKey(raw, repository, id);
        }

        /// <summary>Get a string that represents the current object.</summary>
        public override string ToString()
        {
            return this.LooksValid
                ? $"{this.Repository}:{this.ID}"
                : this.RawText;
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(UpdateKey other)
        {
            return
                other != null
                && this.Repository == other.Repository
                && string.Equals(this.ID, other.ID, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        public override bool Equals(object obj)
        {
            return obj is UpdateKey other && this.Equals(other);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return $"{this.Repository}:{this.ID}".ToLower().GetHashCode();
        }
    }
}
