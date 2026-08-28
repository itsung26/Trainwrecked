using Godot;
using System;

public partial class SessionButton : Button
{
	public string AssignedAddress { get; set; }
	public int AssignedPort { get; set; }

	public void Configure(string ipString, int port)
	{
		AssignedAddress = ipString;
		AssignedPort = port;

		Text = "IP: " + AssignedAddress + "                          " + "PORT: " + AssignedPort;
		
	}

    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
	}

}
