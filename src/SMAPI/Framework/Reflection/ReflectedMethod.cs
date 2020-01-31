using System;
using System.Reflection;

namespace StardewModdingAPI.Framework.Reflection
{
    /// <summary>A method obtained through reflection.</summary>
    internal class ReflectedMethod : IReflectedMethod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type that has the method.</summary>
        private readonly Type ParentType;

        /// <summary>The object that has the instance method (if applicable).</summary>
        private readonly object Parent;

        /// <summary>The display name shown in error messages.</summary>
        private string DisplayName => $"{this.ParentType.FullName}::{this.MethodInfo.Name}";


        /*********
        ** Accessors
        *********/
        /// <summary>The reflection metadata.</summary>
        public MethodInfo MethodInfo { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="parentType">The type that has the method.</param>
        /// <param name="obj">The object that has the instance method(if applicable).</param>
        /// <param name="method">The reflection metadata.</param>
        /// <param name="isStatic">Whether the method is static.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="parentType"/> or <paramref name="method"/> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="obj"/> is null for a non-static method, or not null for a static method.</exception>
        public ReflectedMethod(Type parentType, object obj, MethodInfo method, bool isStatic)
        {
            // validate
            if (parentType == null)
                throw new ArgumentNullException(nameof(parentType));
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (isStatic && obj != null)
                throw new ArgumentException("A static method cannot have an object instance.");
            if (!isStatic && obj == null)
                throw new ArgumentException("A non-static method must have an object instance.");

            // save
            this.ParentType = parentType;
            this.Parent = obj;
            this.MethodInfo = method;
        }

        /// <summary>Invoke the method.</summary>
        /// <typeparam name="TValue">The return type.</typeparam>
        /// <param name="arguments">The method arguments to pass in.</param>
        public TValue Invoke<TValue>(params object[] arguments)
        {
            // invoke method
            object result;
            try
            {
                result = this.MethodInfo.Invoke(this.Parent, arguments);
            }
            catch (TargetParameterCountException)
            {
                throw new Exception($"Couldn't invoke the {this.DisplayName} method: it expects {this.MethodInfo.GetParameters().Length} parameters, but {arguments.Length} were provided.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't invoke the {this.DisplayName} method", ex);
            }

            // cast return value
            try
            {
                return (TValue)result;
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Can't convert the return value of the {this.DisplayName} method from {this.MethodInfo.ReturnType.FullName} to {typeof(TValue).FullName}.");
            }
        }

        /// <summary>Invoke the method.</summary>
        /// <param name="arguments">The method arguments to pass in.</param>
        public void Invoke(params object[] arguments)
        {
            // invoke method
            try
            {
                this.MethodInfo.Invoke(this.Parent, arguments);
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't invoke the {this.DisplayName} method", ex);
            }
        }
    }
}
