using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Schematic.Core.Mvc
{
    public class ResourceControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var schematicAssembly = Assembly.Load("Schematic");
            var candidates = new List<Type>();

            foreach (var assemblyName in schematicAssembly.GetReferencedAssemblies()) 
            {
                var assembly = Assembly.Load(assemblyName);
                var exportedTypes = assembly.GetExportedTypes()
                    .Where(type => type.GetCustomAttributes<SchematicResourceAttribute>().Any());
                    
                foreach (var type in exportedTypes) 
                {
                    candidates.Add(type);
                }
            }
                
            foreach (var candidate in candidates)
            {
                string typeName = candidate.Name;

                Type filterType;
                filterType = schematicAssembly.GetType("Schematic.Filters." + typeName + "Filter");

                if (filterType == null)
                {
                    filterType = typeof(ResourceFilterModel<>).MakeGenericType(candidate).GetTypeInfo();
                }

                feature.Controllers.Add(typeof(ResourceController<,>).MakeGenericType(candidate, filterType).GetTypeInfo());
            }
        }
    }
}