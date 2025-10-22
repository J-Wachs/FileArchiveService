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
public class FileArchiveController(
    ILogger<FileArchiveController> logger,
    IFileArchiveStorage fileStorage,
    IFileArchiveFileInfoCRUD fileInfoCRUD,
    IFileArchiveJWTokenHelperRead fileArchiveJWTokenHelperRead
    ) : ControllerBase
{
    private const long Zero = 0;

    /// <summary>
    /// Download a file. The token contains the Id of the file to download from the file archive.
    /// </summary>
    /// <param name="token">Json Web Token containing information about user and the file to download</param>
    /// <returns></returns>
    [Route("DownloadFile")]
    [HttpGet]
    public async Task<IResult> DownloadFile(string token)
    {
        string methodName = nameof(DownloadFile), paramList = $"('{token}')";

        try
        {
            // Get the User Id and File Id from the token:
            Result<UserIdAndFileIdDTO?> getUserIdAndFileIdResult = fileArchiveJWTokenHelperRead.GetUserIdAndFileIdFromJWToken(token);
            if (getUserIdAndFileIdResult.IsSuccess is false)
            {
                return getUserIdAndFileIdResult.AsIResult();
            }

            var rcdUserIdFileId = getUserIdAndFileIdResult.Data;
            if (rcdUserIdFileId!.UserId is Zero || rcdUserIdFileId.FileId is Zero)
            {
                logger.LogError("Error in '{methodName}{paramList}'. The error is: 'User id or File id not present in token'.", methodName, paramList);
                return Result.FailureBadRequest("User id or File id not present in token").AsIResult();
            }

            // You should implement a verification of the User Id here and bail out
            // if the User Id is not valid, for whatever reason.

            // Is User Id valid?????

            // End User Id verification

            Result<FileArchiveInfo?> getFileInfoResult = await fileInfoCRUD.GetFileInfoById(rcdUserIdFileId.FileId);
            if (getFileInfoResult.IsSuccess is false)
            {
                return getFileInfoResult.AsIResult();
            }

            var infoAboutFile = getFileInfoResult.Data;

            Result<Stream?> openStoredFileResult = await fileStorage.OpenStoredFile(rcdUserIdFileId.FileId);
            if (openStoredFileResult.IsSuccess is false)
            {
                // 'Forbidden' is a special case here. It means that the file is not yet released.
                // As most browsers will display this message to the user, we do not want to use the Result object:
                if (openStoredFileResult.ResultCode is ResultCode.Forbidden)
                {
                    return Results.Text(openStoredFileResult.Messages.FirstOrDefault(), statusCode: StatusCodes.Status403Forbidden);
                }

                return openStoredFileResult.AsIResult();
            }

            var wStream = new StreamHelper().SetFileStream(openStoredFileResult.Data!);

            return Results.File(wStream, infoAboutFile!.FileMimeType!, infoAboutFile.Filename);
        }
        catch (SecurityTokenMalformedException ex)
        {
            logger.LogError("Error in '{methodName}{paramList}'. The error is: 'SecurityTokenMalformedException exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.FailureBadRequest("Token is invalid").AsIResult();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal("An error occurred.").AsIResult();
        }
    }
}
