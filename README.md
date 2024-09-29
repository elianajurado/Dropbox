# Dropbox task solution

Hello! This project consists of two parts: a **SyncServer** and a **SyncMonitorClient**. The idea is that the client monitors a folder (the source) for any changes, and whenever a file is created or modified, it sends the changes over TCP to the server. The server then saves the file in a destination folder.

## Getting Started

To get this running on your machine, you'll need a few things. 

I built this on Windows using .NET Framework 4.7.2, so you'll want to have that version installed, along with Visual Studio 2019 (or higher). I wasn’t able to test it on macOS or Linux, so I can’t guarantee it works there.

First, clone the repo and then open the solution in Visual Studio, and you’re all set.

## Running the Server

The server is super simple to run. Just open the `SyncServer` project, build it, and hit run. It will start listening on `127.0.0.1` on port `8080`, waiting for files to be uploaded by the client. By default, the server saves files in a folder on my desktop (`C:\Users\ElianaJurado\Desktop\SyncedFiles`). You might want to change this in the code to suit your setup, the change has to be made on the appsettings.json.

Once the server is up, it will show messages when a client connects and when files are received.

Code for run the server: dotnet run --project SyncServer

## Running the Client

The client is where the action happens. It monitors a folder for changes (file creation, updates, etc.) and sends any new or modified files to the server.

First, make sure to configure the appsettings.json file in the SyncMonitorClient project:
- `DirectoryToMonitor`: The folder you want to keep an eye on.
- `SyncServerIp`: The IP address of the server (default is `127.0.0.1`).
- `SyncServerPort`: The port where the server is listening (default is `8080`).

Once that’s set, you can run the client like this:

Code for rune the client: dotnet run --project SyncMonitorClient 

It will start watching the folder you specified and upload any new files it detects. You’ll also see log messages for any files that get synced.

## A Things to Note

- **Cross-Platform Support**: I didnt made the solution cross-platform, since I didn’t have a way to test it on macOS or Linux. 
  
- **TCP**: Right now, the file transfers happen over plain TCP, which means they aren’t encrypted.

- **Logging and Testing**: I used `NLog` for basic logging, and some output is still handled through `Console.WriteLine`. More advanced logging (like integrating with a cloud-based system) and better automated tests (unit and integration) would be ideal for a production-level solution. Right now, the testing is pretty basic, mainly manual testing.

## What Could Be Improved

There are a few things I would like improve if this were going to be a production-ready solution:

- **TLS Encryption**: Since i'm using a plain TCP the information is not encrypted so i would add the TLS encryption since it would be important for security.
  
- **Cross-Platform Testing**: I would make sure the solution works properly on macOS and Linux since I only make it for Windows.
  
- **File Compression**: Compressing files before sending them over the network could reduce the time it takes to transfer large files.
  
- **Improve Testing**: The testing here is basic and manual. In a real-world project, I’d add unit and integration tests to make sure everything is solid.

## Time Spent

I spent about 9 hours on this project. Most of that time went into setting up the server and client, writing the file synchronization logic, and adding basic logging. I also tried to make it cross-platform, but as I mentioned earlier, I wasn’t able to fully test that part.

If you have any questions or need more information, feel free to reach out!




