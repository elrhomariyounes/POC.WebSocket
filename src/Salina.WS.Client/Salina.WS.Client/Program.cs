// See https://aka.ms/new-console-template for more information

using System;
using System.Net.WebSockets;
using System.Text;

string filePath = "C:\\Users\\Younes\\Downloads\\Enregistrement.wav";
string wsUrl = "wss://localhost:7030/ws";
using (var client = new ClientWebSocket())
{
    await client.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
    var messageSegment = new ArraySegment<byte>(GetSpanBytesFromFile(filePath).ToArray());
    await client.SendAsync(messageSegment, WebSocketMessageType.Binary, true, CancellationToken.None);
    //await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Sent binary message", CancellationToken.None);
}
Span<byte> GetSpanBytesFromFile(string filePath)
{
	using (FileStream stream = File.OpenRead(filePath))
	{
		Span<byte> buffer = new byte[stream.Length];
		stream.Read(buffer);
		return buffer;
	}
}
