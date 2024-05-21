using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Hosting.Internal;
using TryFirst.Data;
using TryFirst.Models;
using TryFirst.Models.DTO;
using Microsoft.AspNetCore.Hosting;
using System.Xml.Linq;

namespace TryFirst.Controllers
{
    [Route("api/VillaAPI")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ILogger<ValuesController> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        public ValuesController(ILogger<ValuesController> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        [HttpGet("/list")]
        public IActionResult FileList()
        {
            try
            {
                var x = FileManager.FileNames();
                _logger.LogInformation("Ok");
                return Ok(x);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("/create-archive")]
        public IActionResult CreateArchive([FromBody] List<string> filenames)
        {
            var x = FileManager.FileNames();
            for(int i = 0; i < filenames.Count; ++i)
            {
                if (!x.Contains(filenames[i]))
                {
                    string msg = "BadRequest: File not found" + filenames[i];
                    _logger.LogError(msg);
                    return BadRequest(msg);
                }
            }
            //--------------------------------------
            List<int> ids = new List<int>(); //id каждого файла
            foreach (string filename in filenames)
            {
                var dict_names = FileManager.Instance().GetNamesIds();
                ids.Add(dict_names[filename]);
            }
            ids.Sort();
            var zip_ids = FileManager.Instance().GetZipIds();
            var hash = ids.GetHashCode();
            if(zip_ids.Contains(hash))
            {
                //архив с таким набором файлов уже создан
                var zip_dict = FileManager.Instance().GetZipDict();
                var zip_id = zip_cache[ids];

            }
            //----------------------------------------
            _logger.LogInformation("NoContent");
            return NoContent();
        }
























        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(IEnumerable<VillaDTO>))]




















        public IActionResult GetVillas()
        {
            _logger.LogInformation("Getting all villas");
            return Ok(VillaStore.VillaList);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VillaDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetVilla(int id)
        {
            if (id == 0)
            {
                _logger.LogError($"Get Error with Id:{id}");
                return BadRequest();
            }
            var villa = VillaStore.VillaList.FirstOrDefault(x => x.Id == id);
            return villa == null ? NotFound() : Ok(villa);

        }
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VillaDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult CreateVilla([FromBody]VillaDTO villaDTO)
        {
            //if (ModelState.IsValid == false)
            //    return BadRequest(ModelState);
            if( VillaStore.VillaList.FirstOrDefault(x => x.Name.ToLower() == villaDTO.Name.ToLower()) != null)
            {
                ModelState.AddModelError("CustomError", "Villa already exists");
                return BadRequest(ModelState);
            }


            if(villaDTO == null)
                return BadRequest(villaDTO);
            if(villaDTO.Id > 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            villaDTO.Id = VillaStore.VillaList.OrderByDescending(x => x.Id).FirstOrDefault().Id + 1;
            VillaStore.VillaList.Add(villaDTO);
            return Ok(villaDTO);
        }
        
        
        [HttpDelete("{id:int}")]
        public IActionResult DeleteVilla(int id)
        {
            if(id == 0)
            {
                return BadRequest();
            }
            var villa = VillaStore.VillaList.FirstOrDefault( x => x.Id == id);
            if(villa == null) 
            {
                return NotFound();
            }
            VillaStore.VillaList.Remove(villa);
            return NoContent();
        }

        [HttpPut("{id:int}",Name = "UpdateVilla")]

        public IActionResult UpdateVilla(int id, [FromBody]VillaDTO villaDTO)
        {
            if(villaDTO == null || villaDTO.Id != id)
            {
                return BadRequest();
            }
            var villa = VillaStore.VillaList.FirstOrDefault(x => x.Id == id);
            villa.Name = villaDTO.Name;
            villa.Sqft = villaDTO.Sqft;
            villa.Sqft = villaDTO.Sqft;

            return NoContent();

        }

        [HttpPatch("{id:int}", Name = "UeVilla")]
        public IActionResult UpdatePartialVilla(int id, JsonPatchDocument<VillaDTO> patchDTO)
        {
            if (patchDTO == null || id == 0)
            {
                return BadRequest();
            }

            var villa = VillaStore.VillaList.FirstOrDefault(x => x.Id == id);
            if(villa == null)
            {
                return BadRequest();
            }
            patchDTO.ApplyTo(villa, ModelState);
            if (!ModelState.IsValid)
                return BadRequest();
            return NoContent();

        }

        [HttpGet("/shutdown")]
        public IActionResult ShutDown()
        {
            _lifetime.StopApplication();
            _logger.LogInformation("Shutting down...");
            return Ok();
        }
    }
}
