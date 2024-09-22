using FileArchive.Utils;
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
	[CascadingParameter]
	private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

	[Parameter]
	public IList<FileArchiveFileInfoUI> Files { get; set; } = [];

	[Parameter]
	public string? FileTypesAccepted { get; set; }

	[Parameter]
	public bool AllowAdd { get; set; }

	[Parameter]
	public bool AllowDelete { get; set; }

	[Parameter]
	public bool AllowUpdate { get; set; }

	[Parameter]
	public bool AllowUpdateOnNew { get; set; }

	[Parameter]
	public bool AllowDownload { get; set; }

	[Parameter]
	public int AllowedNbrOfFiles { get; set; }

	[Parameter]
	public bool AllowSelectMultipleFiles { get; set; }

	[Parameter]
	public bool DisplayDescription { get; set; }

	[Parameter]
	public bool DisplayExistingFiles { get; set; }

	[Parameter]
	public long? MaxFileSize { get; set; }

	[Parameter]
	public string Height { get; set; } = "400px";


	[Inject]
	IJSRuntime? JSRuntime { get; set; }

	[Inject]
	IConfiguration? Config { get; set; }

    [Inject]
    IFileArchiveJWTokenHelperBuild? FileArchiveJWTokenHelperBuild { get; set; }


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
            _maxFileSize = (long)MaxFileSize;
        }

        if (string.IsNullOrEmpty(FileTypesAccepted) is false)
        {
            _fileTypesAccepted = FileTypesAccepted.ToLowerInvariant();
        }

        if (AllowUpdate || AllowUpdateOnNew)
        {
            _displayDescription = true;
        }
        else
        {
            _displayDescription = DisplayDescription;
        }

        if (AllowUpdate)
        {
            _displayExistingFiles = true;
        }
        else
        {
            _displayExistingFiles = DisplayExistingFiles;
        }

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
			var files = Files.Where(x => x.Id is null).Skip(request.StartIndex).Take(request.Count);
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

		foreach (var oneFile in e.GetMultipleFiles())
		{
			Files?.Add(new FileArchiveFileInfoUI
			{
				Filename = oneFile.Name,
				Description = null,
				Delete = false,
				Update = false,
				Insert = true,
				File = oneFile
			});
		}

		if (filesGrid is not null)
		{
			await filesGrid.RefreshDataAsync();
		}
	}


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


	private static void DescriptionChanged(string value, FileArchiveFileInfoUI file)
	{
		file.Description = value;
		if (file.Insert is false)
		{
			file.Update = true;
		}
	}


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


    private async Task AlertDialog(string message)
    {
		if (JSRuntime is not null)
		{
            await JSRuntime.InvokeVoidAsync("alert", $"{message}");
        }
    }
}
