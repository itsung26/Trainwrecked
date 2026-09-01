using System;
using Godot;

public struct LanDiscoverySession
{
	public string Ip { get; }
	public int Port { get; }

	public LanDiscoverySession(string ip, int port)
	{
		Ip = ip;
		Port = port;
	}
}
