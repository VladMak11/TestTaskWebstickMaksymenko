using Microsoft.AspNetCore.Mvc;
using TestTaskWebstick.Models;
using TestTaskWebstick.Models.DTO;

namespace TestTaskWebstick.Controllers
{
    [Route("api/images")]
    [ApiController]
    
    public class ImagesAPIController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<ImageDTO> GetImages()
        {
            return new List<ImageDTO>
            {
                new ImageDTO{ Id = 1, Url="Url"}
            };
        }

        //[HttpPost]
        //[ActionName("CreateImage")]
        //public async Task<ActionResult<Image>> CreateImage([FromBody]Image createImg)
        //{
        //    if(await )
        //}
    }
}
