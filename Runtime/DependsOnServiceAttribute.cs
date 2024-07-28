using System;
using System.Collections.Generic;

namespace Services
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class DependsOnServiceAttribute : Attribute
    {
        public readonly Type[] Dependencies;
        
        public DependsOnServiceAttribute(params Type[] dependencyTypes)
        {
            var validDependencies = new List<Type>();
            
            foreach (var type in dependencyTypes)
            {
                if (type.GetInterface(nameof(IService)) == null)
                    throw new ArgumentException($"Invalid dependency. {type} does not implement {nameof(IService)}.");

                validDependencies.Add(type);
            }
            
            Dependencies = validDependencies.ToArray();
        }
    }
}
