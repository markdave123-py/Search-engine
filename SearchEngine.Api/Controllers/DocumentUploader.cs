using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using SearchEngine.Api.Core.Services;
using MongoDB.Driver;
using SearchEngine.Models;

using SearchEngine.Api.Core.Files;


namespace SearchEngine.Api.Controllers { }
  

  [Route("/api/documents")]
  [ApiController]
  public class DocumentUploader: ControllerBase {
  private readonly CloudStoreManager _cloudStore;
  private readonly IMongoCollection<Document> _documentCollection;
  private readonly FileManager _fileManager;
  public DocumentUploader(
    CloudStoreManager cloudStoreManager, 
    IMongoDatabase database,
    FileManager manager
    ) {
    _cloudStore = cloudStoreManager;
    _documentCollection = database.GetCollection<Document>("Documents");
    _fileManager = manager;
  }


  [HttpPost("upload")]
  public async Task<IActionResult> Handle([FromForm] IFormFile file)
  {
    if (file == null)
    {
      throw new ArgumentNullException(nameof(file), "File cannot be null");
    }

    if (file == null || file.Length == 0)
      return BadRequest("No file provided.");

    var filePath = Path.GetTempFileName();

    FileStream stream;
    using (stream = new FileStream(filePath, FileMode.Create))
    {
      file.CopyTo(stream);
    }

    var uploadParams = new CloudinaryDotNet.Actions.RawUploadParams()
    {
      File = new CloudinaryDotNet.FileDescription(filePath),
      PublicId = Path.GetFileNameWithoutExtension(file.FileName)
    };

    var uploadResult = _cloudStore.UploadFileToCloudinary(uploadParams);

    var doc = new Document
    {
      Url = uploadResult,
      IsIndexed = false,
      Type = FileManager.GetFileType(file.FileName)
    };

    // await _documentCollection.InsertOneAsync(doc);

    _fileManager.ReadDocumentContents(doc, stream);


    return Ok(new { document = doc });
  }
}
