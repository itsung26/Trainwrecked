using System;
using Godot;

[GlobalClass]
public partial class LanDiscoverySession : Resource
{
	[Export]
	public string Ip { get; set; }
	[Export]
	public int Port { get; set; }

	public LanDiscoverySession(string ip, int port)
	{
		Ip = ip;
		Port = port;
	}
}
