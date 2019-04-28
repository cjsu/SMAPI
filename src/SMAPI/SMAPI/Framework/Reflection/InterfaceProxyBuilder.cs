using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace StardewModdingAPI.Framework.Reflection
{
    /// <summary>Generates a proxy class to access a mod API through an arbitrary interface.</summary>
    internal class InterfaceProxyBuilder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The target class type.</summary>
        private readonly Type TargetType;

        /// <summary>The generated proxy type.</summary>
        private readonly Type ProxyType;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The type name to generate.</param>
        /// <param name="moduleBuilder">The CLR module in which to create proxy classes.</param>
        /// <param name="interfaceType">The interface type to implement.</param>
        /// <param name="targetType">The target type.</param>
        public InterfaceProxyBuilder(string name, ModuleBuilder moduleBuilder, Type interfaceType, Type targetType)
        {
            // validate
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            // define proxy type
            TypeBuilder proxyBuilder = moduleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
            proxyBuilder.AddInterfaceImplementation(interfaceType);

            // create field to store target instance
            FieldBuilder targetField = proxyBuilder.DefineField("__Target", targetType, FieldAttributes.Private);

            // create constructor which accepts target instance and sets field
            {
                ConstructorBuilder constructor = proxyBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new[] { targetType });
                ILGenerator il = constructor.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0); // this
                // ReSharper disable once AssignNullToNotNullAttribute -- never null
                il.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0])); // call base constructor
                il.Emit(OpCodes.Ldarg_0);      // this
                il.Emit(OpCodes.Ldarg_1);      // load argument
                il.Emit(OpCodes.Stfld, targetField); // set field to loaded argument
                il.Emit(OpCodes.Ret);
            }

            // proxy methods
            foreach (MethodInfo proxyMethod in interfaceType.GetMethods())
            {
                var targetMethod = targetType.GetMethod(proxyMethod.Name, proxyMethod.GetParameters().Select(a => a.ParameterType).ToArray());
                if (targetMethod == null)
                    throw new InvalidOperationException($"The {interfaceType.FullName} interface defines method {proxyMethod.Name} which doesn't exist in the API.");

                this.ProxyMethod(proxyBuilder, targetMethod, targetField);
            }

            // save info
            this.TargetType = targetType;
            this.ProxyType = proxyBuilder.CreateType();
        }

        /// <summary>Create an instance of the proxy for a target instance.</summary>
        /// <param name="targetInstance">The target instance.</param>
        public object CreateInstance(object targetInstance)
        {
            ConstructorInfo constructor = this.ProxyType.GetConstructor(new[] { this.TargetType });
            if (constructor == null)
                throw new InvalidOperationException($"Couldn't find the constructor for generated proxy type '{this.ProxyType.Name}'."); // should never happen
            return constructor.Invoke(new[] { targetInstance });
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Define a method which proxies access to a method on the target.</summary>
        /// <param name="proxyBuilder">The proxy type being generated.</param>
        /// <param name="target">The target method.</param>
        /// <param name="instanceField">The proxy field containing the API instance.</param>
        private void ProxyMethod(TypeBuilder proxyBuilder, MethodInfo target, FieldBuilder instanceField)
        {
            Type[] argTypes = target.GetParameters().Select(a => a.ParameterType).ToArray();

            // create method
            MethodBuilder methodBuilder = proxyBuilder.DefineMethod(target.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);
            methodBuilder.SetParameters(argTypes);
            methodBuilder.SetReturnType(target.ReturnType);

            // create method body
            {
                ILGenerator il = methodBuilder.GetILGenerator();

                // load target instance
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, instanceField);

                // invoke target method on instance
                for (int i = 0; i < argTypes.Length; i++)
                    il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Call, target);

                // return result
                il.Emit(OpCodes.Ret);
            }
        }
    }
}
