using FileArchive.Models;
using FileArchive.Services;
using FileArchive.Utils;
using FileArchive.Utils.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Data;
using System.Security.Claims;

namespace FileArchive;

public partial class FileArchiveList
{
	/// <summary>
	/// Authentication information.
	/// </summary>
	[CascadingParameter]
	private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

	/// <summary>
	/// Files to manage in the component.
	/// </summary>
	[Parameter]
	public List<FileArchiveFileInfoUI> Files { get; set; } = [];

	/// <summary>
	/// The file types it is allowed to upload. Each file type must be stated with preceeding dot.
	/// The file type must be seperated by comma, e.g.: .jpeg,.jpg,.png
	/// </summary>
	[Parameter]
	public string? FileTypesAccepted { get; set; }

	/// <summary>
	/// Is user allowed to add files to the archive.
	/// </summary>
	[Parameter]
	public bool AllowAdd { get; set; }

    /// <summary>
    /// Is user allowed to delete files from the archive.
    /// </summary>
    [Parameter]
	public bool AllowDelete { get; set; }

    /// <summary>
    /// Is user allowed to update information about files in the archive.
    /// </summary>
	[Parameter]
	public bool AllowUpdate { get; set; }

    /// <summary>
    /// Is user allowed to update information on files just added (not yet submitted).
    /// </summary>
    [Parameter]
	public bool AllowUpdateOnNew { get; set; }

    /// <summary>
    /// Is user allowed to download files.
    /// </summary>
    [Parameter]
	public bool AllowDownload { get; set; }

	/// <summary>
	/// The number of files allowed in the archive for a given parent key.
	/// Please note, that the file archive component does not handle the 
	/// parent key, that is suppose to be done in the form submit method. 
	/// </summary>
	[Parameter]
	public int AllowedNbrOfFiles { get; set; }

	/// <summary>
	/// Is user allowed to select more than one file to upload at a time.
	/// </summary>
	[Parameter]
	public bool AllowSelectMultipleFiles { get; set; }

	/// <summary>
	/// Is the description of each file to be displayed. This apply when displaying
	/// existing files and uploading new files.
	/// </summary>
	[Parameter]
	public bool DisplayDescription { get; set; }

	/// <summary>
	/// Is the component to display existing files in the archive.
	/// </summary>
	[Parameter]
	public bool DisplayExistingFiles { get; set; }

	/// <summary>
	/// The maximun size in bytes that the files added must be.
	/// </summary>
	[Parameter]
	public long? MaxFileSize { get; set; }

	/// <summary>
	/// The height attribute of the file archive component. Default is 400 pixels.
	/// </summary>
	[Parameter]
	public string Height { get; set; } = "400px";


	[Inject]
	IJSRuntime? JSRuntime { get; set; }

	[Inject]
	IConfiguration? Config { get; set; }

    [Inject]
    IFileArchiveJWTokenHelperBuild? FileArchiveJWTokenHelperBuild { get; set; }

	// Value used for user id if authentication attribute cannot be found.
	private string _curUserId = "-1";

	private long _maxFileSize;
	private string _fileTypesAccepted = string.Empty;
	private bool _displayDescription;
	private bool _displayExistingFiles;

	private Virtualize<FileArchiveFileInfoUI>? filesGrid;


	protected override async Task OnInitializedAsync()
	{
		if (AuthenticationStateTask is not null)
		{
			var authState = await AuthenticationStateTask;
            
            if (DoesAuthStateHaveUser(authState))
			{
                if (IsUserAuthenticatedAndHaveClaims(authState))
				{
                    var userId = GetUserIdFromClaims(authState);
					if (userId is not null)
					{
						_curUserId = userId.Value;
					}
				}
			}
		}
	}


    protected override async Task OnParametersSetAsync()
    {
        if (AllowedNbrOfFiles < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(AllowedNbrOfFiles));
        }

        if (MaxFileSize is null)
        {
            if (Config is not null)
            {
                _maxFileSize = ConfigHelper.GetMustExistConfigValue<long>(Config, FileArchiveConstants.ConfigMaxFileSize);
            }
            else
            {
                throw new ArgumentNullException(FileArchiveConstants.ConfigMaxFileSize);
            }
        }
        else
        {
            _maxFileSize = MaxFileSize.Value;
        }

        _fileTypesAccepted = string.IsNullOrEmpty(FileTypesAccepted) is false ? FileTypesAccepted.ToLowerInvariant() : string.Empty;
        _displayDescription = AllowUpdate || AllowUpdateOnNew ? true : DisplayDescription;
        _displayExistingFiles = AllowUpdate ? true : DisplayExistingFiles;
        
        await base.OnParametersSetAsync();
    }


    private static Claim? GetUserIdFromClaims(AuthenticationState authState)
    {
		return authState.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
    }


    private static bool IsUserAuthenticatedAndHaveClaims(AuthenticationState authState)
    {
		return (authState.User.Identity!.IsAuthenticated && authState.User.Claims is not null);
    }


    private static bool DoesAuthStateHaveUser(AuthenticationState authState)
    {
		return (authState is not null && authState.User is not null && authState.User.Identity is not null);
    }


	private ValueTask<ItemsProviderResult<FileArchiveFileInfoUI>> GetRows(ItemsProviderRequest request)
	{
		if (_displayExistingFiles)
		{
			return new(new ItemsProviderResult<FileArchiveFileInfoUI>(
				Files.Skip(request.StartIndex).Take(request.Count),
						   Files.Count));
		}
		else
		{
			// Files added by clicking on the button 'Add files' does not have an Id 
			// in the list, hence this is the rule to display them.
			var files = Files.Where(x => x.Id is null).Skip(request.StartIndex).Take(request.Count).ToList();
			return new(new ItemsProviderResult<FileArchiveFileInfoUI>(
					   files,
					   files.Count()));
		}
	}


	private async void LoadFiles(InputFileChangeEventArgs e)
	{
		// Check file size and type are valid:
		if (AreAllFilesAllowed(e.GetMultipleFiles(), out string errorMessage) is false)
		{
			await AlertDialog(errorMessage);
			return;
		}

		int nbrOfExistingFiles = 0;

		if (Files is not null)
		{
			// Cleanup by removing files that was just added:
			foreach (var oneFile in Files.Where(x => x.Insert).ToList())
			{
				Files.Remove(oneFile);
			}
			nbrOfExistingFiles = Files.Count;
		}

		if (AllowedNbrOfFiles > 0)
		{
			if (AllowedNbrOfFiles < nbrOfExistingFiles + e.FileCount)
			{
                await AlertDialog($"You have selected too many files. There can be a max of {AllowedNbrOfFiles} files in the archive.\n\nThe upload is aborted. Please reselect files and try again.");
                return;
			}
		}

		// Add the files the user has added
		foreach (var oneFile in e.GetMultipleFiles())
		{
			Files?.Add(new FileArchiveFileInfoUI
			{
				Filename = oneFile.Name,
				Description = null,
				Delete = false,
				Insert = true,
				Update = false,
				File = oneFile
			});
		}

		await RefreshData();
	}

    /// <summary>
    /// Force the Virtualize component to update: 
    /// </summary>
    /// <returns></returns>
    public async Task RefreshData()
	{
        if (filesGrid is not null)
        {
            await filesGrid.RefreshDataAsync();
        }
    }

	/// <summary>
	/// Manage that files are of allowed type and have a max size allowed.
	/// </summary>
	/// <param name="browserFiles">Files from user</param>
	/// <param name="errorMessage">Error message to be displayed</param>
	/// <returns></returns>
    private bool AreAllFilesAllowed(IReadOnlyList<IBrowserFile> browserFiles, out string errorMessage)
	{
		bool allFileTypesAllowed = AreAllFileTypesAllowed(browserFiles, out string ftErrorMessage);
		bool allFileSizeAllowed = AreAllFilesSizesAllowed(browserFiles, out string fsErrorMessage);

		if (allFileTypesAllowed is false || allFileSizeAllowed is false)
		{
			errorMessage = ftErrorMessage + fsErrorMessage + "The upload is aborted. Please reselect files and try again.";
			return false;
		}

		errorMessage = string.Empty;
		return true;
	}

    /// <summary>
    /// Does all new files have the allowed extensions.
    /// </summary>
    /// <param name="browserFiles">Files from user</param>
	/// <param name="errorMessage">Error message to be displayed</param>
    /// <returns></returns>
    private bool AreAllFileTypesAllowed(IReadOnlyList<IBrowserFile> browserFiles, out string errorMessage)
	{
		errorMessage = string.Empty;
		bool atLeastOneInvalidFileType = false;
		int numberOfInvalidFileTypes = 0;

		if (string.IsNullOrEmpty(_fileTypesAccepted) is false)
		{
			foreach (var file in browserFiles)
			{
				FileInfo fileInfo = new (file.Name);
				if (_fileTypesAccepted.Contains(fileInfo.Extension, StringComparison.InvariantCultureIgnoreCase) is false)
				{
					errorMessage += $"{file.Name}\n";
					atLeastOneInvalidFileType = true;
				}
			}
		}

		if (atLeastOneInvalidFileType)
		{
			if (numberOfInvalidFileTypes == 1)
			{
				errorMessage = $"This file have an invalid extension (allowed file extensions are '{_fileTypesAccepted}'):\n" + errorMessage + "\n";
			}
			else
			{
				errorMessage = $"These files have invalid extensions (allowed file extentions are '{_fileTypesAccepted}'):\n" + errorMessage + "\n";
			}
		}

		return !atLeastOneInvalidFileType;
	}

    /// <summary>
    /// Does all files have a size allowed.
    /// </summary>
    /// <param name="browserFiles">Files from user</param>
    /// <param name="errorMessage">Error message to be displayed</param>
    /// <returns></returns>
    private bool AreAllFilesSizesAllowed(IReadOnlyList<IBrowserFile> browserFiles, out string errorMessage)
	{
		errorMessage = string.Empty;
		bool atLeastOneFileIsTooLarge = false;
		int numberOfTooLargeFiles = 0;

		foreach (var file in browserFiles)
		{
			if (file.Size > _maxFileSize)
			{
				errorMessage += $"{file.Name}: {file.Size} bytes\n";
				atLeastOneFileIsTooLarge = true;
				numberOfTooLargeFiles++;
			}
		}

		if (atLeastOneFileIsTooLarge)
		{
			if (numberOfTooLargeFiles == 1)
			{
				errorMessage = $"This file is to large to upload (the max size is {_maxFileSize} bytes):\n" + errorMessage + "\n";
			}
			else
			{
				errorMessage = $"These files are to large to upload (the max size for each file is {_maxFileSize} bytes):\n" + errorMessage + "\n";
			}
		}

		return !atLeastOneFileIsTooLarge;
	}

	/// <summary>
	/// Remove a file that was previously added by user, but has no been submitted yet.
	/// </summary>
	/// <param name="file">The new file to remove</param>
	/// <returns></returns>
	private async Task RemoveNewFile(FileArchiveFileInfoUI file)
	{
		var fileEntry = Files.SingleOrDefault(x =>
		{
			IBrowserFile? file1 = file?.File;
			if (x is null || x.File is null)
			{
				return false;
			}
			return x.File.Equals(file1);
		});

		if (fileEntry is not null)
		{
			Files.Remove(fileEntry);
		}
		if (filesGrid is not null)
		{
			await filesGrid.RefreshDataAsync();
		}
	}

	/// <summary>
	/// Update the description on a file in archive.
	/// </summary>
	/// <param name="newDescription">The new description</param>
	/// <param name="file">The file that must have the new description</param>
	private static void DescriptionChanged(string newDescription, FileArchiveFileInfoUI file)
	{
		file.Description = newDescription;
		if (file.Insert is false)
		{
			file.Update = true;
		}
	}

	/// <summary>
	/// Download a file from the archive.
	/// </summary>
	/// <param name="file">The file to download</param>
	/// <exception cref="Exception">In case of no info about file</exception>
	private async void DownloadFile(FileArchiveFileInfoUI file)
	{
		if (Config is null || file is null || file.Id is null)
		{
			throw new Exception();
		}

		if (JSRuntime is not null)
		{
			if (file is not null && file.Id is not null)
			{
				var result = FileArchiveJWTokenHelperBuild!.BuildTokenForFileDownload(_curUserId, (long)file.Id);
				if (result.IsSuccess)
				{
					await JSRuntime.InvokeVoidAsync("open", $"/api/FileArchive/DownloadFile?token={result.Data}", "_blank");
				}
			}
		}
	}

	/// <summary>
	/// Displays a JavaScript alert box with a message.
	/// </summary>
	/// <param name="message">The message to display</param>
	/// <returns></returns>
    private async Task AlertDialog(string message)
    {
		if (JSRuntime is not null)
		{
            await JSRuntime.InvokeVoidAsync("alert", $"{message}");
        }
    }
}
