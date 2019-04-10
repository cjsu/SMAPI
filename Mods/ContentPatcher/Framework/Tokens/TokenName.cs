using System;
using ContentPatcher.Framework.Conditions;

namespace ContentPatcher.Framework.Tokens
{
    /// <summary>Represents a token key and subkey if applicable (e.g. <c>Relationship:Abigail</c> is token key <c>Relationship</c> and subkey <c>Abigail</c>).</summary>
    internal struct TokenName : IEquatable<TokenName>, IComparable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The token type.</summary>
        public string Key { get; }

        /// <summary>The token subkey indicating which in-game object the condition type applies to, if applicable. For example, the NPC name when <see cref="Key"/> is <see cref="ConditionType.Relationship"/>.</summary>
        public string Subkey { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="tokenKey">The condition type.</param>
        /// <param name="subkey">A unique key indicating which in-game object the condition type applies to. For example, the NPC name when <paramref name="tokenKey"/> is <see cref="ConditionType.Relationship"/>.</param>
        public TokenName(string tokenKey, string subkey = null)
        {
            this.Key = tokenKey?.Trim();
            this.Subkey = subkey?.Trim();
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="tokenKey">The condition type.</param>
        /// <param name="subkey">A unique key indicating which in-game object the condition type applies to. For example, the NPC name when <paramref name="tokenKey"/> is <see cref="ConditionType.Relationship"/>.</param>
        public TokenName(ConditionType tokenKey, string subkey = null)
            : this(tokenKey.ToString(), subkey) { }

        /// <summary>Get a string representation for this instance.</summary>
        public override string ToString()
        {
            return this.HasSubkey()
                ? $"{this.Key}:{this.Subkey}"
                : this.Key;
        }

        /// <summary>Get whether this key has the same root <see cref="Key"/> as another.</summary>
        /// <param name="other">The other key to check.</param>
        public bool IsSameRootKey(TokenName other)
        {
            if (this.Key == null)
                return other.Key == null;

            return this.Key.Equals(other.Key, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>Whether this token key specifies a subkey.</summary>
        public bool HasSubkey()
        {
            return !string.IsNullOrWhiteSpace(this.Subkey);
        }

        /// <summary>Try to parse the <see cref="Key"/> as a global condition type.</summary>
        /// <param name="type">The parsed condition type, if applicable.</param>
        public bool TryGetConditionType(out ConditionType type)
        {
            return Enum.TryParse(this.Key, true, out type);
        }

        /// <summary>Get the root token (without the <see cref="Subkey"/>).</summary>
        public TokenName GetRoot()
        {
            return this.HasSubkey()
                ? new TokenName(this.Key)
                : this;
        }

        /****
        ** IEquatable
        ****/
        /// <summary>Get whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(TokenName other)
        {
            return this.CompareTo(other) == 0;
        }

        /// <summary>Get whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        public override bool Equals(object obj)
        {
            return obj is TokenName other && this.Equals(other);
        }

        /// <summary>Get the hash code for this instance.</summary>
        public override int GetHashCode()
        {
            return this.ToString().ToLowerInvariant().GetHashCode();
        }

        /****
        ** IComparable
        ****/
        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order.</returns>
        public int CompareTo(object obj)
        {
            return string.Compare(this.ToString(), obj?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /****
        ** Static parsing
        ****/
        /// <summary>Parse a raw string into a condition key if it's valid.</summary>
        /// <param name="raw">The raw string.</param>
        /// <returns>Returns true if <paramref name="raw"/> was successfully parsed, else false.</returns>
        public static TokenName Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentNullException(nameof(raw));

            // extract parts
            string key;
            string subkey;
            {
                string[] parts = raw.Trim().Split(new[] { ':' }, 2);

                key = parts[0].Trim();
                if (key == "")
                    throw new ArgumentException($"The main key in '{raw}' can't be blank.");

                subkey = parts.Length == 2 ? parts[1].Trim() : null;
                if (subkey == "")
                    subkey = null;
            }

            // create instance
            return new TokenName(key, subkey);
        }

        /// <summary>Parse a raw string into a condition key if it's valid.</summary>
        /// <param name="raw">The raw string.</param>
        /// <param name="key">The parsed condition key.</param>
        /// <returns>Returns true if <paramref name="raw"/> was successfully parsed, else false.</returns>
        public static bool TryParse(string raw, out TokenName key)
        {
            try
            {
                key = TokenName.Parse(raw);
                return true;
            }
            catch
            {
                key = default(TokenName);
                return false;
            }
        }
    }
}
