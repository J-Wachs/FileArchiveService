# File Archive service

The File Archive service handles everything needed to store and retrieve files from an archive. It consist of a Blazor
component for displaying and maintaining the content of the file archive. Further, it contains classes to support the
Blazor visual component and maintaining the file archive programatically.

In the File Archive, files are grouped by a 'parent key'. What that key is, is up to you. It could be a incident report Id,
an order number or something completly different.

The visual component can be configured to match the needs in the specific case.

The service has been developed in .NET 8.

## Fully functional example project

This repo contains a project that is the actual components, it's support classes and a demo project, where you can see
examples of various configurations of the component:

* Allow users to add files to a the File Archive
* Allow users to see files already in the File Archive
* Allow users to enter/maintain a description of the files in the File Archive
* Allow users to delete files in the File Archive
* Only display files and allow download

The File Archive consist of two parts:
* Information about the files in the File Archive
* Storage of the actual files

The component project contains classes to store the information about files in either a JSON file (should only be used in
these demos) or in a database using Entity Framework. Further, the project contains a class to store the files either in
a folder on the harddrive or in Azure Blob Storage (or Azurite Blob Storage).

## How does it work?

There are four parts that make the File Archive service:

* Method to read information about the files already in the File Archive
* Component to maintain the File Archive
* Method to store changes made to information about files and store the files themselfs
* Web API to download files

Internally when a user uploads files, the files are stored under an Id. The original filename is only stored as a part
of the information about the file. This prohibit that users can try to do malicious things by giving files bad names.

## Installation instructions

### Getting and trying out the File Archive service
Download the repo and open the demo project in Visual Studio. The repo is set up to store information about files in a
JSON file. This is only for demo purpose for a single user. It will not work in a multiuser environment. Further, the
demo project is configured to store the actual files in the folder 'C:\FileArchiveStorage'. You must create this folder
before running the demo project. If the folder does not exist, an exception is thrown. Play around with the demo.

Please note, that the demo project is prepared for using Data First with Entity Framework, should you wish the store
information on a SQL server.

### Setting up your own project to use the File Archive service
To use the File Archive service in your own projects, you must add the service project to your solution. Then you must
decide how files are to be stored. There are to classes for that. One that will store files in a folder on the harddrive,
and one that will store the files in Azure Blob storage. Then you need to add a number of services to the Program.cs in
your main project. Example from demo project:

// Added for FileArchive:
builder.Services.AddControllers();
builder.Services.AddScoped<IFileArchiveStorage, FileArchiveStorageFolder>();
//builder.Services.AddScoped<IFileArchiveStorage, FileArchiveFolderAzureBlob>();
builder.Services.AddScoped<IFileArchiveFileInfoCRUD, FileArchiveFileInfoCRUDJSON>();
//builder.Services.AddScoped<IFileArchiveFileInfoCRUD, FileArchiveFileInfoCRUDDB>();
builder.Services.AddScoped<IJWTokenHelper, JWTokenHelper>();
builder.Services.AddScoped<IFileArchiveJWTokenHelper, FileArchiveJWTokenHelper>();

The two services at bottom, are for the Java Web Token security that is used when downloading files.

### Adding and using the File Archive component in your own projects
Add the File Archive component to the relevant pages in your project. If the page is an Update page, you
must add code to retrieve information about the files in the archive (see demos). This list is passed to the File Archive
component, in order for the user to maintain the File Archive.

In the method that handles a successful submit request (the OnValidSubmit property on the EditForm tag), you must call
the method that will maintain the File Archive and possibly upload new files. Please take a closer look at the code in the
demos and copy the necessary code over.

As files are to be stored under a 'parent key', your OnValidSubmit method must establish the parent key in create
situations.

Please note that depending on the config of the File Archive component, the user can download the files in the archive.

## Modifying the File Archive service for your own use

Allowing users to upload files opens a risk of virus infected files entering your system. You should definitely consider
extending the upload part, have it call a virus scanner. Which method that is the right one for you, is something that
you must figure out.

There are services that expose a virus scanner API that you can call. When using the option to store files on a folder,
make sure that the virus scanner on the server scans the folder in question.

Your organisation can have other requirements that are not covered by my project. Please feel free to adapt a local 
version to fit your needs.

Remember to change the passphrase for the signing of the Java Web Token.

## Found a bug?

Please create an issue in the repo.

## Known issues (Work in progress)

### No virus scanner integration
File Archive does not have any virus scanner integration build in. Do not expect for one to come.

### Authorized users and Java Web Token for download API
The JWT made for the download to protect the API, simply receives the user identification, it is not validated. If you 
will be using the File Archive service, consider to extend the API to validate against your actual identify provider.

Also it will be required to make sure the user id is correctly retrieved in the File Archive component when it
collects the user id.

### Connection string to Azure blob storage
Both the File Archive component and the API class get the connection string from the settings file. The setting required
needs the complete connection string. It should be redesigned to use azure valut and store the id info there.

## FAQ

### I want to use the component in an application that have tenants, what do I do?
To use the File Archive service in a multi tenant environment, you must extent the class FileArchiveInfo.cs in the folder
Models. You must add a field to identify the Tenant. After adding a field, you must add the needed code to obtain info
about the tenant, and making sure, this information is passed though the service.

### How do I get all the Parent Keys that are used when storing files?
Currently there is no functionality to do that. 

### How do I use OIOSAML/SAML to validate the user and the download?
You will have to read the OIOSAML/SAML documentation and possibly find a OIOSAML/SAML framework that will do the hard work.
However, you can take the needed parts from this service and use it, e.g. the download service etc.

### I have pages where I just want to display information about the files, not download. Do I need the File Archive Component?
You have two options:
* Use the File Archive component, and set the config to not allow adding, updating and download
* Or, just loop over the list of file information that is returned from the call to 'fileArchiveCRUD.GetListOfFileInfoForArchive(parentKey);'
 and write the relevant informtion to the screen.
 