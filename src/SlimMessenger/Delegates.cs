namespace SlimMessenger;

// server

public delegate void ServerEventHandler(SlimServer server);

// client

public delegate void ClientEventHandler(SlimClient client);
public delegate void DataReceivedEventHandler(SlimClient client, string message);
