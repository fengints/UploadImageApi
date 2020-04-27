﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UploadImage.CInterface;
using UploadImage.Models;
using UploadImage.Utils;

namespace UploadImage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageResizer resizer;
        private readonly ILogger<ImageController> _logger;
        private readonly IImageService _service;
        private readonly ImageContext _dbContext;

        public ImageController(ILogger<ImageController> logger, IImageService service, ImageContext dbContext)
        {
            resizer =  new ImageResizer(100, 100, ImageFormat.Jpeg);
            _logger = logger;
            _service = service;
            _dbContext = dbContext;
        }




        //// POST: api/Image
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> PostJson([Required][FromBody] JsonBase64ImageInfo[] imageInfos)
        {
            try
            {
                //Save File
                foreach (var imageInfo in imageInfos)
                {
                    await _service.SecurityCheck(imageInfo);

                    var dbModel = new ImageDbModel()
                    {
                        FileName = imageInfo.fileName,
                        AddedDate = DateTime.Now,
                        ImageData = imageInfo.data,
                        PreviewData = resizer.Resize(imageInfo.data),
                    };
                    await _service.Save(dbModel, _dbContext);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }

            return Ok();
        }

        
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostForm([Required] IFormCollection dataCollection)
        {
            try
            {
                if (dataCollection.Files.Count == 0)
                {
                    return BadRequest("Request has no files");
                }

                foreach (var formFile in dataCollection.Files)
                {
                    await using (var stream = new MemoryStream(2048))
                    {
                        //Get data from stream
                        await formFile.CopyToAsync(stream);
                        var content = stream.ToArray();

                        //Check and save
                        await _service.SecurityCheck(new ImageInfo() {fileName = formFile.FileName, data = content });

                        ImageDbModel model = new ImageDbModel()
                        {
                            FileName = formFile.FileName,
                            AddedDate = DateTime.Now,
                            ImageData = content,
                            PreviewData = resizer.Resize(content),
                        };
                        await _service.Save(model, _dbContext);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }

            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> PostFromUrl([Url] string url, string fileName)
        {
            if (string.IsNullOrEmpty(url))
                return BadRequest();
            Uri uri = new Uri(url);

            try
            {
                using var client = new WebClient();
                var imageBytes = await client.DownloadDataTaskAsync(uri);

                ImageInfo imageInfo = new ImageInfo() {fileName = "", data = imageBytes };

                await _service.SecurityCheck(imageInfo); 
                
                ImageDbModel model = new ImageDbModel()
                {
                    FileName = fileName,
                    AddedDate = DateTime.Now,
                    ImageData = imageBytes,
                    PreviewData = resizer.Resize(imageBytes),
                };
                await _service.Save(model, _dbContext);

                return Ok();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }

        }
    }
}