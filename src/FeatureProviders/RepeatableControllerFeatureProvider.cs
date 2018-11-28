using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Schematic.Core;
using Schematic.Controllers;

namespace Schematic.Core.Mvc
{
    public class RepeatableControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var candidates = new List<Type>();
            
            var exportedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type.GetCustomAttributes<SchematicRepeatableAttribute>().Any());
                    
            foreach (var type in exportedTypes)
            {
                candidates.Add(type);
            }
                
            foreach (var candidate in candidates)
            {
                feature.Controllers.Add(typeof(RepeatableController<>).MakeGenericType(candidate).GetTypeInfo());
            }
        }
    }
}