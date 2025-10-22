using FileArchive.Models;
using FileArchive.Utils.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Data;
using System.Security.Claims;

namespace FileArchive;

public partial class FileArchiveCards(
    ILogger<FileArchiveCards> logger
    ) : IDisposable
{
    /// <summary>
    /// Authentication information.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    /// <summary>
    /// The edit context from the parent.
    /// </summary>
    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    /// <summary>
    /// Files to manage in the component.
    /// </summary>
    [Parameter, EditorRequired]
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
    private int _secondsBeforeReleaseOfFile = 0;
    private ValidationMessageStore? _messageStore;
    private DateTime _earlistReleaseOfFile = DateTime.Now;
    private Timer? _reloadTimer;

    protected override async Task OnInitializedAsync()
    {
        if (Config is not null)
        {
            _secondsBeforeReleaseOfFile = Config.GetValue("FileArchive:SecondsBeforeReleaseOfFile", 0);
        }

        if (CurrentEditContext is not null)
        {
            _messageStore = new(CurrentEditContext);
        }

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
        string methodName = nameof(OnParametersSetAsync), paramList = "()";

        if (AllowedNbrOfFiles < 0)
        {
            logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'AllowedNbrOfFiles cannot be less than zero'.", methodName, paramList);
            throw new ArgumentOutOfRangeException("FileArchive:AllowedNbrOfFiles");
        }

        if (MaxFileSize is null)
        {
            if (Config is null)
            {
                logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'Config object is null'.", methodName, paramList);
                throw new ArgumentOutOfRangeException("Config");
            }

            _maxFileSize = Config.GetValue<long>("FileArchive:MaxFileSize");

            if (_maxFileSize < 0)
            {
                logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'AllowedNbrOfFiles cannot be less than zero'.", methodName, paramList);
                throw new ArgumentNullException("FileArchive:MaxFileSize");
            }
        }
        else
        {
            _maxFileSize = MaxFileSize.Value;
        }

        _fileTypesAccepted = string.IsNullOrEmpty(FileTypesAccepted) is false ? FileTypesAccepted.ToLowerInvariant() : string.Empty;
        _displayDescription = AllowUpdate || AllowUpdateOnNew || DisplayDescription;
        _displayExistingFiles = AllowUpdate || DisplayExistingFiles;

        await base.OnParametersSetAsync();
    }

    /// <summary>
    /// Retrieves the user ID claim from the specified authentication state.
    /// </summary>
    /// <param name="authState">The authentication state containing the user's claims.</param>
    /// <returns>The <see cref="Claim"/> representing the user ID if found; otherwise, <see langword="null"/>.</returns>
    private static Claim? GetUserIdFromClaims(AuthenticationState authState)
    {
        return authState.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Determines whether the user is authenticated and has associated claims.
    /// </summary>
    /// <param name="authState">The authentication state containing the user's identity and claims.</param>
    /// <returns><see langword="true"/> if the user is authenticated and has claims; otherwise, <see langword="false"/>.</returns>
    private static bool IsUserAuthenticatedAndHaveClaims(AuthenticationState authState)
    {
        return (authState.User.Identity!.IsAuthenticated && authState.User.Claims is not null);
    }

    /// <summary>
    /// Determines whether the specified <see cref="AuthenticationState"/> contains a valid user identity.
    /// </summary>
    /// <param name="authState">The authentication state to evaluate. Cannot be <c>null</c>.</param>
    /// <returns><see langword="true"/> if the <paramref name="authState"/> contains a non-null user with a valid identity;
    /// otherwise, <see langword="false"/>.</returns>
    private static bool DoesAuthStateHaveUser(AuthenticationState authState)
    {
        return (authState is not null && authState.User is not null && authState.User.Identity is not null);
    }

    /// <summary>
    /// Force the component to re-render.
    /// </summary>
    public Task RefreshData()
    {
        // StateHasChanged() is the correct way to force a UI update for @foreach.
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes the files selected by the user for upload, validating their size, type, and count, and adds them to
    /// the file archive.
    /// </summary>
    /// <remarks>This method performs the following actions: <list type="bullet"> <item> Validates that all
    /// selected files meet the allowed size and type constraints. If validation fails, an error message is sent, and
    /// the operation is aborted. </item> <item> Ensures that the total number of files, including the newly selected
    /// ones, does not exceed the allowed limit. If the limit is exceeded, an error message is sent, and the operation
    /// is aborted. </item> <item> Adds valid files to the file archive with metadata such as creation timestamp and
    /// file name. </item> </list> After processing, the method refreshes the data to reflect the updated file
    /// archive.</remarks>
    /// <param name="eventArgs">The event arguments containing the files selected by the user.</param>
    private async void GetFilesToUpload(InputFileChangeEventArgs eventArgs)
    {
        // Check file size and type are valid:
        if (AreAllFilesAllowed(eventArgs.GetMultipleFiles(), out string errorMessage) is false)
        {
            await SendMessage(errorMessage);
            return;
        }

        int nbrOfExistingFiles = Files is null ? 0 : Files.Count;

        if (AllowedNbrOfFiles > 0 && AllowedNbrOfFiles < (nbrOfExistingFiles + eventArgs.FileCount))
        {
            errorMessage = $"You have selected too many files. There can be a max of {AllowedNbrOfFiles} files in the archive." +
                "\n\nThe upload is aborted. Please reselect files and try again.";
            await SendMessage(errorMessage);
            return;
        }

        // Add the files the user has added
        foreach (var oneFile in eventArgs.GetMultipleFiles())
        {
            Files?.Add(new FileArchiveFileInfoUI
            {
                Created = DateTime.Now, // Set the creation timestamp
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

    // New method to dispose the timer when the component is removed.
    public void Dispose()
    {
        _reloadTimer?.Dispose();
        GC.SuppressFinalize(this); 
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
                FileInfo fileInfo = new(file.Name);
                if (_fileTypesAccepted.Contains(fileInfo.Extension, StringComparison.InvariantCultureIgnoreCase) is false)
                {
                    errorMessage += $"{file.Name}\n";
                    atLeastOneInvalidFileType = true;
                    numberOfInvalidFileTypes++;
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
                errorMessage = $"This file is too large to upload (the max size is {_maxFileSize} bytes):\n" + errorMessage + "\n";
            }
            else
            {
                errorMessage = $"These files are too large to upload (the max size for each file is {_maxFileSize} bytes):\n" + errorMessage + "\n";
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
        var fileEntry = Files.SingleOrDefault(x => x.File != null && x.File.Equals(file.File));

        if (fileEntry is not null)
        {
            Files.Remove(fileEntry);
        }
        await RefreshData();
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
        string methodName = nameof(DownloadFile), paramList = "(file)";

        if (Config is null || file is null || file.Id is null)
        {
            logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'One or more of Config/file/file.Id is null'.", methodName, paramList);
            throw new ArgumentNullException(nameof(file));
        }

        if (JSRuntime is not null && FileArchiveJWTokenHelperBuild is not null)
        {
            if (file.Id is not null)
            {
                var buildTokenResult = FileArchiveJWTokenHelperBuild.BuildTokenForFileDownload(_curUserId, (long)file.Id);
                if (buildTokenResult.IsSuccess)
                {
                    await JSRuntime.InvokeVoidAsync("open", $"/api/FileArchive/DownloadFile?token={buildTokenResult.Data}", "_blank");
                }
                else
                {
                    var errorMessage = buildTokenResult.Messages is not null ? string.Join(", ", buildTokenResult.Messages) : "No error message";
                    logger.LogError("Error in '{methodName}{paramList}'. The error is: '{errorMessage}'.", methodName, paramList, errorMessage);

                    await SendMessage(errorMessage);
                }
            }
        }
    }

    /// <summary>
    /// Displays a JavaScript alert box with a message or adds message
    /// to EditForm message store.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <returns></returns>
    private async Task SendMessage(string message)
    {
        string methodName = nameof(SendMessage), paramList = "(message)";

        if (CurrentEditContext is null)
        {
            if (JSRuntime is not null)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"{message}");
            }
            else
            {
                logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'JSRuntime is null'.", methodName, paramList);
            }
        }
        else
        {
            WriteErrorMessageToMessageStore(message);
        }
    }

    /// <summary>
    /// Write error message to the message store of the edit context.
    /// </summary>
    /// <param name="errorMessage">The error message to write to store</param>
    private void WriteErrorMessageToMessageStore(string errorMessage)
    {
        string methodName = nameof(WriteErrorMessageToMessageStore), paramList = "(errorMessage)";

        if (CurrentEditContext is not null)
        {
            var fieldIdentifier = new FieldIdentifier(CurrentEditContext.Model, nameof(Files));
            _messageStore?.Clear(fieldIdentifier);
            _messageStore?.Add(fieldIdentifier, errorMessage);

            CurrentEditContext!.NotifyValidationStateChanged();
        }
        else
        {
            logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'CurrentEditContext is null'.", methodName, paramList);
        }
    }

    /// <summary>
    /// The calculated property that replaces the filtering logic from GetRows.
    /// The @foreach loop in the razor file will iterate over this.
    /// </summary>
    private IEnumerable<FileArchiveFileInfoUI> FilesToDisplay
    {
        get
        {
            // The timer setup is called here to preserve the original logic
            // of re-evaluating the timer whenever the view is rendered.
            if (Files is null)
            {
                return [];
            }

            SetupReloadTimer();

            if (_displayExistingFiles)
            {
                return Files;
            }
            return Files.Where(x => x.Id is null);
        }
    }

    /// <summary>
    /// This method contains the timer logic, extracted from the old GetRows method. 
    /// </summary>
    private void SetupReloadTimer()
    {
        if (Config is not null)
        {
            _earlistReleaseOfFile = DateTime.Now.AddSeconds(-_secondsBeforeReleaseOfFile);
        }
        _reloadTimer?.Dispose();

        var newestUnreleasedFile = Files
            .Where(f => f.Created > _earlistReleaseOfFile)
            .OrderByDescending(f => f.Created)
            .FirstOrDefault();

        if (newestUnreleasedFile is not null && newestUnreleasedFile.Created is not null)
        {
            var releaseTime = newestUnreleasedFile.Created.Value.AddSeconds(_secondsBeforeReleaseOfFile);
            var delay = releaseTime - DateTime.Now;
            if (delay > TimeSpan.Zero)
            {
                _reloadTimer = new Timer(async _ =>
                {
                    await InvokeAsync(StateHasChanged);
                }, null, delay, Timeout.InfiniteTimeSpan);
            }
        }
    }
}
