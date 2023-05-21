﻿using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        const int MAX_SIZE_IN_BYTES_PIC = 5 * 1024 * 1024; //МБ

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
                    var cc = response.Content.Headers.ContentType.ToString();
                     var tt = GetFileExtensionFromContentType(cc);
                    return tt;
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
        //[ProducesResponseType(StatusCodes.Status200OK)]
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
                    Image image = new Image 
                    { 
                        Id = _db.Images.Count() > 0 ? _db.Images.OrderByDescending(x => x.Id).FirstOrDefault().Id + 1 : 0,
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

        [HttpGet("get-url")]
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
            string url = $"{Request.Scheme}://{Request.Host}/{objImage.Url}";
            return Ok(new { url });
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
