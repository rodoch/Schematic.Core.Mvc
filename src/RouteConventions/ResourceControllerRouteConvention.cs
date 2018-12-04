using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Schematic.Core.Mvc
{
    public class ResourceControllerRouteConvention : IControllerModelConvention
    {
        protected string DetermineResourceControllerName(Type genericType, SchematicResourceAttribute attribute)
        {
            return (attribute != null && attribute.ControllerName.HasValue()) 
                ? attribute.ControllerName
                : genericType.Name;
        }

        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.IsGenericType)
            {
                var genericType = controller.ControllerType.GenericTypeArguments[0];
                var customNameAttribute = genericType.GetCustomAttribute<SchematicResourceAttribute>();
                
                controller.ControllerName = DetermineResourceControllerName(genericType, customNameAttribute);
    
                if (customNameAttribute != null && customNameAttribute.Route.HasValue())
                {
                    controller.Selectors.Add(new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(customNameAttribute.Route)),
                    });
                }
            }
        }
    }
}