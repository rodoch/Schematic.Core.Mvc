using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using Schematic.Core;

namespace Schematic.Core.Mvc
{
    [Route("{culture}/resource/[controller]")]
    [Authorize]
    public class ResourceController<T, TResourceFilter> : Controller 
        where T : class, new()
        where TResourceFilter : IResourceFilter<T>, new()
    {
        protected readonly IConfiguration Configuration;
        protected readonly IResourceRepository<T, TResourceFilter> ResourceRepository;

        public ResourceController(
            IConfiguration configuration,
            IResourceRepository<T, TResourceFilter> resourceRepository)
        {
            Configuration = configuration;
            ResourceRepository = resourceRepository;
        }
        
        public static string ResourceType = typeof(T).Name.ToLower();

        protected ClaimsIdentity ClaimsIdentity => User.Identity as ClaimsIdentity;
        protected int UserID => int.Parse(ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        [HttpGet]
        public virtual IActionResult Explorer(int id = 0, string facets = "")
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            var explorer = new ResourceExplorerModel()
            {
                ResourceID = id,
                ResourceType = ResourceType,
                Facets = facets.GetFacets()
            };

            string resourceName = typeof(T).GetAttributeValue((SchematicResourceNameAttribute r) => r.Name);

            ViewData["ResourceName"] = resourceName;
            ViewData["ContentWebPath"] = Configuration["AppSettings:ContentWebPath"];

            return View(explorer);
        }

        [Route("create")]
        [HttpGet]
        public virtual IActionResult Create()
        {
            ViewData["ContentWebPath"] = Configuration["AppSettings:ContentWebPath"];

            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }
            
            var result = new ResourceModel<T>() 
            { 
                Resource = new T()
            };

            return PartialView("_Editor", result);
        }

        [Route("create")]
        [HttpPost]
        public virtual IActionResult Create(ResourceModel<T> data)
        {
            ViewData["ContentWebPath"] = Configuration["AppSettings:ContentWebPath"];

            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }
            
            if (!ModelState.IsValid)
            {
                return PartialView("_Editor", data);
            }

            int newResourceID = ResourceRepository.Create(data.Resource, UserID);

            if (newResourceID == 0)
            {
                return NoContent();
            }

            string controllerName = ControllerContext.RouteData.Values["controller"].ToString();

            return Created(Url.Action("Read", controllerName, new { id = newResourceID }), newResourceID);
        }

        [Route("read")]
        [HttpGet("{id:int}")]
        public virtual IActionResult Read(int id, string facets = "")
        {
            ViewData["ContentWebPath"] = Configuration["AppSettings:ContentWebPath"];

            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            T resource = ResourceRepository.Read(id);

            if (resource == null)
            {
                return NotFound();
            }
            
            var result = new ResourceModel<T>()
            { 
                ResourceID = id,
                Resource = resource,
                Facets = facets.GetFacets()
            };

            return PartialView("_Editor", result);
        }

        [Route("update")]
        [HttpPost]
        public virtual IActionResult Update(ResourceModel<T> data)
        {
            ViewData["ContentWebPath"] = Configuration["AppSettings:ContentWebPath"];

            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }
            
            if (!ModelState.IsValid)
            {
                return PartialView("_Editor", data);
            }

            bool update = ResourceRepository.Update(data.Resource, UserID);

            if (!update)
            {
                return BadRequest();
            }

            T updatedResource = ResourceRepository.Read(data.ResourceID);
            
            var result = new ResourceModel<T>() 
            { 
                ResourceID = data.ResourceID,
                Resource = updatedResource 
            };

            return PartialView("_Editor", result);
        }

        [Route("delete")]
        [HttpPost]
        public virtual IActionResult Delete(int id)
        {   
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            bool delete = ResourceRepository.Delete(id, UserID);

            if (!delete)
            {
                return BadRequest();
            }

            return NoContent();
        }

        [Route("filter")]
        [HttpGet]
        public virtual IActionResult Filter(string facets = "")
        {
            ViewData["ContentWebPath"] = Configuration["AppSettings:ContentWebPath"];

            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            TResourceFilter filter = new TResourceFilter()
            {
                Facets = facets.GetFacets()
            };

            return PartialView("_ResourceFilter", filter);
        }

        [Route("list")]
        [HttpPost]
        public virtual IActionResult List(TResourceFilter filter)
        {
            ViewData["ContentWebPath"] = Configuration["AppSettings:ContentWebPath"];
            
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            List<T> list = ResourceRepository.List(filter);

            if (list.Count == 0)
            {
                return NoContent();
            }

            var resourceList = new ResourceListModel<T>()
            {
                List = list,
                ActiveResourceID = filter.ActiveResourceID
            };

            return PartialView("_ResourceList", resourceList);
        }

        [Route("schema")]
        [HttpGet]
        public virtual async Task<IActionResult> Schema()
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            var schema = await JsonSchema4.FromTypeAsync<T>();
            var schemaData = schema;
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            return Json(schemaData, serializerSettings);
        }
    }
}