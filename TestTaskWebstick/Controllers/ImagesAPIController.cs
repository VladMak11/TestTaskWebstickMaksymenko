using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TestTaskWebstick.Data;
using TestTaskWebstick.Models;
using TestTaskWebstick.Models.DTO;

namespace TestTaskWebstick.Controllers
{
    [Route("api/images")]
    [ApiController]
    
    public class ImagesAPIController : ControllerBase
    {
        private readonly ApplicationDBContext _db;
        private static readonly object _lockObject = new object();

        public ImagesAPIController(ApplicationDBContext db)
        {
            _db = db;
        }
        private bool UrlExists(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task<string> DownloadImageToStorageByUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                byte[] imgData = await client.GetByteArrayAsync(url);

                string fileName, filePath;
                fileName = Guid.NewGuid().ToString();
                filePath = Path.Combine("images", fileName);
                System.IO.File.WriteAllBytes(filePath, imgData);
                return filePath;
            }
               
        }

        [HttpPost("upload-by-url")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadImageByUrl([FromBody] ImageCreateRequest imageobj)
        {
            if(imageobj == null)
            {
                return BadRequest(imageobj);
            }
            string imageUrl = imageobj.Url;

            if (!UrlExists(imageUrl))
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Invalid image URL");
            }

            //TODO - size PIC = 5*1024*1024 req=400

            lock (_lockObject)
            {
                try
                {
                    
                    Image image = new Image 
                    { 
                        Id = _db.Images.Count() > 0 ? _db.Images.OrderByDescending(x => x.Id).FirstOrDefault().Id + 1 : 0,
                        Url =  DownloadImageToStorageByUrl(imageUrl).Result
                };
                     _db.Images.Add(image);
                     _db.SaveChanges();

                    return Ok(new { url = image.Url});
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error uploading image: {ex.Message}");
                }
            }
        }


        [HttpGet("get-url")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Image>> GetImageUrl(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var objImage =  await _db.Images.FirstOrDefaultAsync(x => x.Id == id);
              
            if (objImage == null)
            {
                return NotFound();
            }
            return Ok(objImage);
        }


        //[HttpGet]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<ActionResult<IEnumerable<Image>>> GetImages()
        //{
        //    return Ok(await _db.Images.ToListAsync());
        //}

        //[HttpGet("{id:int}", Name = "GetImage")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<ImageDTO>> GetImage(int id)
        //{
        //    if (id == 0)
        //    {
        //        return BadRequest();
        //    }
        //    var objImage = ImageStore.imageList.FirstOrDefault(x => x.Id == id);
        //    if (objImage == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(objImage);
        //}

        //[HttpPost(Name = "CreateImage")]
        //[ProducesResponseType(StatusCodes.Status201Created)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]

        //public async Task<ActionResult<Image>> CreateImage([FromBody] ImageDTO imageDTOobj)
        //{
        //    if (imageDTOobj == null)
        //    {
        //        return BadRequest(imageDTOobj);
        //    }
        //    if (imageDTOobj.Id > 0)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError);
        //    }
        //    imageDTOobj.Id = ImageStore.imageList.OrderByDescending(x => x.Id).FirstOrDefault().Id + 1;
        //    //ImageStore.imageList.Add(imageDTOobj);


        //    return Ok(imageDTOobj);
        //}

        //[HttpDelete("{id:int}", Name = "Remove")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult> Remove(int id)
        //{
        //    if (id == 0)
        //    {
        //        return BadRequest();
        //    }
        //    var image = ImageStore.imageList.FirstOrDefault(x => x.Id == id);
        //    if (image == null)
        //    {
        //        return NotFound();
        //    }
        //    ImageStore.imageList.Remove(image);
        //    return NoContent();
        //}


        //[HttpPut("{id:int}", Name = "UpdateImage")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult> UpdateImage(int id, [FromBody] ImageDTO imageDTOobj)
        //{
        //    if (imageDTOobj == null || id != imageDTOobj.Id)
        //    {
        //        return BadRequest();
        //    }
        //    var image = ImageStore.imageList.FirstOrDefault(x => x.Id == id);
        //    image.Url = imageDTOobj.Url;

        //    return NoContent();
        //}
    }
}
