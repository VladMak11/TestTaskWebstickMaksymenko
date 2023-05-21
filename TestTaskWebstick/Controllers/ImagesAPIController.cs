using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TestTaskWebstick.Data;
using TestTaskWebstick.Models;
using TestTaskWebstick.Models.DTO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


namespace TestTaskWebstick.Controllers
{
    [Route("api/images")]
    [ApiController]
    
    public class ImagesAPIController : ControllerBase
    {
        private readonly ApplicationDBContext _db;
        private static readonly object _lockObject = new object();
        const int MAX_SIZE_IN_BYTES_PIC = 5 * 1024 * 1024; //МБ
        const int AVAILABLE_CONVERT_SIZE_1 = 100;
        const int AVAILABLE_CONVERT_SIZE_2 = 300;

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
       
        private async Task<string> DownloadImageToStorageByUrl(string url, string formatPic)
        {
            using (HttpClient client = new HttpClient())
            {
                byte[] imgData = await client.GetByteArrayAsync(url);

                string fileName, filePath;
                fileName = Guid.NewGuid().ToString()+$".{formatPic}";
                filePath = Path.Combine("images", fileName);
                System.IO.File.WriteAllBytes(filePath, imgData);
                return filePath;
            }
               
        }
        private async Task<bool> IsImageUrlValid(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                return  response.IsSuccessStatusCode && response.Content.Headers.ContentType.MediaType.StartsWith("image/");
            }
        }
        private async Task<string> currentFormatPic(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    //var cc = response.Content.Headers.ContentType.ToString();
                    // var tt = GetFileExtensionFromContentType(cc);
                    return GetFileExtensionFromContentType(response.Content.Headers.ContentType.ToString());
                }
            }
            return null; 
        }
        private string GetFileExtensionFromContentType(string contentType)
        {
            string pattern = @"image/(.+)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(contentType);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
        private async Task<bool> CheckImageSizeByUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    byte[] data = await response.Content.ReadAsByteArrayAsync();
                    if (data.Length <= MAX_SIZE_IN_BYTES_PIC)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
       

        [HttpPost("upload-by-url")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadImageByUrl([FromBody] ImageCreateRequest imageobj)
        {
            if(imageobj == null)
            {
                return BadRequest(imageobj);
            }
            string imageUrl = imageobj.Url;

            if (!UrlExists(imageUrl) || !IsImageUrlValid(imageUrl).Result)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Invalid image URL");
            }

            if (!CheckImageSizeByUrl(imageUrl).Result)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Image more 5 MB");
            }

            lock (_lockObject)
            {
                
                try
                {
                    ImageModel image = new ImageModel
                    { 
                        Id = _db.Images.Count() > 0 ? _db.Images.OrderByDescending(x => x.Id).FirstOrDefault().Id + 1 : 1,
                        Url =  DownloadImageToStorageByUrl(imageUrl, currentFormatPic(imageUrl).Result).Result
                    };
                     _db.Images.Add(image);
                     _db.SaveChanges();
                    string url = $"{Request.Scheme}://{Request.Host}/{image.Url}";
                    return Ok(new { url });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error uploading image: {ex.Message}");
                }
            }
        }

        [HttpGet("get-url/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImageUrl(int id)
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
            if (!System.IO.File.Exists(objImage.Url))
            {
                return NotFound("Image with specific url does not exist.");
            }
            string url = $"{Request.Scheme}://{Request.Host}/{objImage.Url}";
            return Ok(new { url });
        }

        [HttpGet("get-url/{id:int}/size/{size:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImageThumnailUrl(int id, int size)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var objImage = await _db.Images.FirstOrDefaultAsync(x => x.Id == id);

            if (objImage == null)
            {
                return NotFound();
            }
            if (!System.IO.File.Exists(objImage.Url))
            {
                return NotFound("Image with specific url does not exist.");
            }
            if(size != AVAILABLE_CONVERT_SIZE_1 && size != AVAILABLE_CONVERT_SIZE_2)
            {
                return BadRequest("Unavalaible size");
            }
            string imagePath = objImage.Url;
            int desiredSize = size;
            string fileName = Path.GetFileNameWithoutExtension(imagePath);
            string extension = Path.GetExtension(imagePath);
            string thumbnailPath =$"images/{fileName}_{size}x{size}{extension}";
            if (!System.IO.File.Exists(thumbnailPath))
            {
                using (Image image = Image.Load(imagePath))
                {
                    image.Mutate(x => x.Resize(desiredSize, desiredSize));
                    image.Save(thumbnailPath);
                }
            }
            return Ok(thumbnailPath);
        }

        [HttpDelete("remove/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Remove(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var objImage = await _db.Images.FirstOrDefaultAsync(x => x.Id == id);
            if (objImage == null)
            {
                return NotFound();
            }
            string removeImagePath = objImage.Url;
            if (!System.IO.File.Exists(removeImagePath))
            {
                return NotFound();
            }
            string fileName = Path.GetFileNameWithoutExtension(removeImagePath);
            string[] files = Directory.GetFiles("images", $"*{fileName}*");

            foreach (string file in files)
            {
                System.IO.File.Delete(file);
            }

            
            _db.Images.Remove(objImage);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
