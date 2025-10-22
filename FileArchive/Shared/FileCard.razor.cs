using FileArchive.Models;
using Microsoft.AspNetCore.Components;

namespace FileArchive.Shared;

public partial class FileCard
{
    /// <summary>
    /// File to manage in the card.
    /// </summary>
    [Parameter, EditorRequired]
    public FileArchiveFileInfoUI File { get; set; } = default!;

    /// <summary>
    /// The earliest release date/time of the file. This date/time, if used, allows for 
    /// the antivirus scan to complete before allowing download.
    /// </summary>
    [Parameter]
    public DateTime EarliestReleaseOfFile { get; set; }

    /// <summary>
    /// Is the user allowed to download the file?
    /// </summary>
    [Parameter]
    public bool AllowDownload { get; set; }

    /// <summary>
    /// Is the Decription of the file to be displayed?
    /// </summary>
    [Parameter]
    public bool DisplayDescription { get; set; }

    /// <summary>
    /// Is the user allowed to update the description of the file?
    /// </summary>
    [Parameter]
    public bool CanUpdateDescription { get; set; }

    /// <summary>
    /// Is the user allowed to delete-mark the file?
    /// </summary>
    [Parameter]
    public bool AllowDelete { get; set; }

    /// <summary>
    /// If add is allowed in the parent component, the user must be allowed to remove the added file.
    /// </summary>
    [Parameter]
    public bool AllowAdd { get; set; }

    /// <summary>
    /// Gets or sets the callback that is invoked when a file download action is triggered.
    /// </summary>
    /// <remarks>Use this property to handle file download events in the UI. The callback is triggered with
    /// the file information when a download event occurs.</remarks>
    [Parameter]
    public EventCallback<FileArchiveFileInfoUI> OnDownload { get; set; }

    /// <summary>
    /// Gets or sets the callback that is invoked when a file remove action is triggered.
    /// </summary>
    [Parameter]
    public EventCallback<FileArchiveFileInfoUI> OnRemove { get; set; }

    /// <summary>
    /// Gets or sets the callback that is invoked when the description changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDescriptionChanged { get; set; }
}
