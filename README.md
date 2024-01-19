# Chat Application with C#
[Video Presentation](https://youtu.be/4Q7ajgXOnTs)

## Description
For the Systems Programming module in my 2nd year of Software Engineering BEng I have created a Non-Persistent Chat Application using C# Sockets.

## How it works
The client has UI made using Spectre.Console that is re-generated each time a message is received with the prompt for a new message being cancelled using a cancellation token,
so that the UI thread can close out successfuly upon switching to the newly generated UI. Message and User objects are serialized into JSON and passed through sockets between the client and server by encoding
them into a byte array.

## Struggles
- ManualResetEvents, controlling a threads state was quite tricky to get my head around at first as I had to think about how each method,
receiving, sending, and UI rendering would run and whether I want that thread to be able to continue processing data or to stop it.

- CancellationTokens, a big part of this application is the ability for the user to have their UI update with any new messages that come in.
However, due to Console.ReadLine() and other text prompt methods blocking the thread to wait for input I had to be able to invalidate these prompts in order to allow the client to safely close out to the home page
without leaving background UI threads running that need to be manually closed by the user upon disconnection from the server.
