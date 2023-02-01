using System.Net;

namespace SlimMessenger;

// server

public delegate void ServerEventHandler(SlimServer server);

// client

public delegate void ClientEventHandler(SlimClient client);
public delegate Task ClientRunLoop(SlimClient client);

public delegate void ClientConnectedToEndPointEventHandler(SlimClient client, bool success, IPAddress serverIP, int serverPort);
