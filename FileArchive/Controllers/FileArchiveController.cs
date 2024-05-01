using FileArchive.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FileArchive.Controllers
{
	/// <summary>
	/// This is a base FileController API made for downloadning files in the 
	/// file archive.
 	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	public class FileArchiveController(IFileArchiveStorage fileStorage, IFileArchiveFileInfoCRUD fileInfoCRUD, IFileArchiveJWTokenHelperRead fileArchiveJWTokenHelperRead) : ControllerBase
    {
        private const long Zero = 0;
        /// <summary>
        /// Download a file. The token contains the Id of the file to download from the file archive.
        /// </summary>
        /// <param name="token">Java Web Token containing information about user and the file to download</param>
        /// <returns></returns>
		[Route("DownloadFile")]
		[HttpGet]
		public IActionResult DownloadFile(string token)
		{
            try
            {
                // Get the User Id and File Id from the token:
                var rcdUserIdFileId = fileArchiveJWTokenHelperRead.GetUserIdAndFileIdFromJWToken(token);
                if (rcdUserIdFileId.UserId is Zero || rcdUserIdFileId.FileId is Zero)
                {
                    return new BadRequestResult();
                }

                // You should implement a verification of the User Id here and bail out
                // if the User Id is not valid, for whatever reason.

                // Is User Id valid?????

                // End User Id verification

                var infoAboutFile = fileInfoCRUD?.GetFileInfoById(rcdUserIdFileId.FileId);
                if (infoAboutFile is null)
                {
                    return new BadRequestResult();
                }

                Stream? stream = fileStorage.OpenStoredFile(rcdUserIdFileId.FileId).GetAwaiter().GetResult();
                var wStream = new StreamHelper().CreateFileStream(stream);

                return File(wStream, infoAboutFile.FileMimeType, infoAboutFile.Filename);

            }
            catch(SecurityTokenMalformedException)
            {
                return new BadRequestObjectResult("Token is invalid");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"Exception: '{ex}'");
            }
        }
    }
}
