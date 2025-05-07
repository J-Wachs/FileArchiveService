using FileArchive.Interfaces;
using FileArchive.Models;
using FileArchive.Utils;
using FileArchive.Utils.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static FileArchive.Utils.FileArchiveJWTokenHelperRead;

namespace FileArchive.Controllers;

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
    public async Task<IActionResult> DownloadFile(string token)
    {
        try
        {
            // Get the User Id and File Id from the token:
            Result<UserIdAndFileIdDTO?> getUserIdAndFileIdResult = fileArchiveJWTokenHelperRead.GetUserIdAndFileIdFromJWToken(token);
            if (getUserIdAndFileIdResult.IsSuccess is false)
            {
                return BadRequest(getUserIdAndFileIdResult);
            }

            var rcdUserIdFileId = getUserIdAndFileIdResult.Data;
            if (rcdUserIdFileId!.UserId is Zero || rcdUserIdFileId.FileId is Zero)
            {
                return BadRequest(getUserIdAndFileIdResult);
            }

            // You should implement a verification of the User Id here and bail out
            // if the User Id is not valid, for whatever reason.

            // Is User Id valid?????

            // End User Id verification

            Result<FileArchiveInfo?> getFileInfoResult = await fileInfoCRUD.GetFileInfoById(rcdUserIdFileId.FileId);
            if (getFileInfoResult.IsSuccess is false)
            {
                return NotFound(getFileInfoResult);
            }

            var infoAboutFile = getFileInfoResult.Data;

            Result<Stream?> openStoredFileResult = await fileStorage.OpenStoredFile(rcdUserIdFileId.FileId);
            if (openStoredFileResult.IsSuccess is false)
            {
                return NotFound(openStoredFileResult);
            }

            var wStream = new StreamHelper().SetFileStream(openStoredFileResult.Data!);

            return File(wStream, infoAboutFile!.FileMimeType, infoAboutFile.Filename);

        }
        catch (SecurityTokenMalformedException)
        {
            return new BadRequestObjectResult("Token is invalid");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult($"An exception occurred: '{ex}'");
        }
    }
}
