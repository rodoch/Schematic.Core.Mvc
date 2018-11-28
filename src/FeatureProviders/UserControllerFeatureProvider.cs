using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Schematic.Controllers;
using Schematic.Core;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public class UserControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var candidates = new List<Type>();
            
            var exportedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type.GetCustomAttributes<SchematicUserAttribute>().Any());
            
            //var exportedTypes = Assembly.GetExecutingAssembly().GetExportedTypes()
            //    .Where(type => type.GetCustomAttributes<SchematicUserAttribute>().Any());
                
            foreach (var type in exportedTypes) 
            {
                candidates.Add(type);
            }
            
            feature.Controllers.Add(typeof(UserController<>).MakeGenericType(candidates[0]).GetTypeInfo());
        }
    }
}