namespace TcpServerSlim;

// server

public delegate void ServerEventHandler(TcpServerSlim server);

// client

public delegate void ClientEventHandler(TcpClientSlim client);
public delegate void DataReceivedEventHandler(TcpClientSlim client, string message);
