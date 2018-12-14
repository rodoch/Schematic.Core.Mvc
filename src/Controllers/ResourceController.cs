using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;

namespace Schematic.Core.Mvc
{
    [Route("{culture}/resource/[controller]")]
    [Authorize]
    public class ResourceController<T, TFilter> : Controller 
        where T : class, new()
        where TFilter : IResourceFilter<T>, new()
    {
        protected readonly IOptionsMonitor<SchematicSettings> Settings;
        protected readonly IResourceRepository<T, TFilter> ResourceRepository;
        protected readonly IResourceContext<T> Context;
        protected ClaimsIdentity ClaimsIdentity => User.Identity as ClaimsIdentity;
        protected int UserID => int.Parse(ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        public ResourceController(
            IOptionsMonitor<SchematicSettings> settings,
            IResourceRepository<T, TFilter> resourceRepository,
            IResourceContext<T> context)
        {
            Settings = settings;
            ResourceRepository = resourceRepository;
            Context = context;
        }
        
        public static string ResourceType = typeof(T).GetAttributeValue((SchematicResourceAttribute r) => r.ControllerName).HasValue() 
            ? typeof(T).GetAttributeValue((SchematicResourceAttribute r) => r.ControllerName).ToLower()
            : typeof(T).Name.ToLower();

        [HttpGet]
        public virtual IActionResult Explorer(int id = 0, string name = "", string facets = "")
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            var explorer = new ResourceExplorerModel()
            {
                ResourceID = id,
                ResourceType = ResourceType,
                Facets = facets
            };

            string resourceName = typeof(T).GetAttributeValue((SchematicResourceNameAttribute r) => r.Name);
            resourceName = (name.HasValue()) ? name : resourceName;
            ViewData["ResourceName"] = resourceName;

            return View(explorer);
        }

        [Route("meta")]
        [HttpGet]
        public virtual IActionResult Meta(int id)
        {
            // TODO: Move ID and title from Read action to Meta action

            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            return Json(new {});
        }

        [Route("create")]
        [HttpGet]
        public virtual IActionResult Create(string facets = "")
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }
            
            var result = new ResourceModel<T>() 
            {
                Resource = new T(),
                Facets = facets
            };

            result = Context.OnPrepare(result);

            return PartialView("_Editor", result);
        }

        [Route("create")]
        [HttpPost]
        public async virtual Task<IActionResult> CreateAsync(ResourceModel<T> data)
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            data = Context.OnPrepare(data);
            
            if (!ModelState.IsValid)
            {
                return PartialView("_Editor", data);
            }

            int newResourceID = await ResourceRepository.CreateAsync(data.Resource, UserID);

            if (newResourceID == 0)
            {
                return NoContent();
            }

            string controllerName = ControllerContext.RouteData.Values["controller"].ToString();
            return Created(Url.Action("Read", controllerName, new { id = newResourceID }), newResourceID);
        }

        [Route("read")]
        [HttpGet("{id:int}")]
        public async virtual Task<IActionResult> ReadAsync(int id, string facets = "")
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            var resource = await ResourceRepository.ReadAsync(id);

            if (resource == null)
            {
                return NotFound();
            }
            
            var result = new ResourceModel<T>()
            { 
                ResourceID = id,
                Resource = resource,
                Facets = facets
            };

            result = Context.OnPrepare(result);

            return PartialView("_Editor", result);
        }

        [Route("update")]
        [HttpPost]
        public async virtual Task<IActionResult> UpdateAsync(ResourceModel<T> data)
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            data = Context.OnPrepare(data);
            
            if (!ModelState.IsValid)
            {
                return PartialView("_Editor", data);
            }

            int update = await ResourceRepository.UpdateAsync(data.Resource, UserID);

            if (update <= 0)
            {
                return BadRequest();
            }

            var updatedResource = await ResourceRepository.ReadAsync(data.ResourceID);
            
            var result = new ResourceModel<T>() 
            { 
                ResourceID = data.ResourceID,
                Resource = updatedResource 
            };

            result = Context.OnReturn(result);

            return PartialView("_Editor", result);
        }

        [Route("delete")]
        [HttpPost]
        public async virtual Task<IActionResult> DeleteAsync(int id)
        {   
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            int delete = await ResourceRepository.DeleteAsync(id, UserID);

            if (delete <= 0)
            {
                return BadRequest();
            }

            return NoContent();
        }

        [Route("filter")]
        [HttpGet]
        public virtual IActionResult Filter(string facets = "")
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            var filter = new TFilter()
            {
                Facets = facets
            };

            return PartialView("_ResourceFilter", filter);
        }

        [Route("list")]
        [HttpPost]
        public async virtual Task<IActionResult> ListAsync(TFilter filter)
        {   
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            List<T> list = await ResourceRepository.ListAsync(filter);

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
        public virtual async Task<IActionResult> SchemaAsync()
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