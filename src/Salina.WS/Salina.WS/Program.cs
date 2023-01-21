using System;
using System.Net.WebSockets;
using System.Text;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAzureClients((azureBuilder) =>
{
    var storageSection = builder.Configuration.GetSection("StorageAccount");
    string connectionString = storageSection.Value!;
    azureBuilder.AddBlobServiceClient(connectionString);
});

var app = builder.Build();

//Use WebSockets
app.UseWebSockets();
app.Map("/ws", async (context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var blobServiceClient = context.RequestServices.GetRequiredService<BlobServiceClient>();
        await ReceiveMessage(webSocket, blobServiceClient);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.Run();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

static async Task Upload(byte[] buffer, BlobServiceClient blobServiceClient)
{
    var containerClient = blobServiceClient.GetBlobContainerClient("audios");
    var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString() + ".wav");
    await blobClient.UploadAsync(new MemoryStream(buffer));
}
static async Task ReceiveMessage(WebSocket webSocket, BlobServiceClient blobServiceClient)
{
    List<byte> buffer = new List<byte>(2 * 1024 * 1024);
    CancellationTokenSource cts = new CancellationTokenSource();
    while (true)
    {
        if (cts.IsCancellationRequested)
        {
            break;
        }
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer.ToArray()),
            CancellationToken.None);
        buffer.AddRange(buffer.Take(receiveResult.Count));
        if (receiveResult.EndOfMessage)
        {
            byte[] receivedMessage = buffer.ToArray();
            await Upload(receivedMessage, blobServiceClient); //Upload File
            buffer.Clear();
            buffer.Capacity = 2 * 1024 * 1024;
            cts.Cancel();
        }
    }
}
