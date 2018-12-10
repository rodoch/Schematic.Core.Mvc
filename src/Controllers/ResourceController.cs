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
    public class ResourceController<T, TResourceFilter> : Controller 
        where T : class, new()
        where TResourceFilter : IResourceFilter<T>, new()
    {
        protected readonly IOptionsMonitor<SchematicSettings> Settings;
        protected readonly IResourceRepository<T, TResourceFilter> ResourceRepository;
        protected ClaimsIdentity ClaimsIdentity => User.Identity as ClaimsIdentity;
        protected int UserID => int.Parse(ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        public ResourceController(
            IOptionsMonitor<SchematicSettings> settings,
            IResourceRepository<T, TResourceFilter> resourceRepository)
        {
            Settings = settings;
            ResourceRepository = resourceRepository;
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

        // TODO: Move ID and title from Read to Meta actions
        [Route("meta")]
        [HttpGet]
        public virtual IActionResult Meta(int id)
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }

            return Json(new {});
        }

        [Route("create")]
        [HttpGet]
        public virtual IActionResult Create()
        {
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
        public async virtual Task<IActionResult> CreateAsync(ResourceModel<T> data)
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }
            
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

            T resource = await ResourceRepository.ReadAsync(id);

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
        public async virtual Task<IActionResult> UpdateAsync(ResourceModel<T> data)
        {
            if (!User.IsAuthorized(typeof(T))) 
            {
                return Unauthorized();
            }
            
            if (!ModelState.IsValid)
            {
                return PartialView("_Editor", data);
            }

            int update = await ResourceRepository.UpdateAsync(data.Resource, UserID);

            if (update <= 0)
            {
                return BadRequest();
            }

            T updatedResource = await ResourceRepository.ReadAsync(data.ResourceID);
            
            var result = new ResourceModel<T>() 
            { 
                ResourceID = data.ResourceID,
                Resource = updatedResource 
            };

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

            TResourceFilter filter = new TResourceFilter()
            {
                Facets = facets
            };

            return PartialView("_ResourceFilter", filter);
        }

        [Route("list")]
        [HttpPost]
        public async virtual Task<IActionResult> ListAsync(TResourceFilter filter)
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