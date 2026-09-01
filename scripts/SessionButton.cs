using Godot;
using System;

public partial class SessionButton : Button
{
	public string AssignedAddress { get; set; }
	public int AssignedPort { get; set; }

	// Configures a hosted session display button.
	// Call after adding to the tree first.
	public void Configure(string ipString, int port)
	{
		if (!IsInsideTree())
		{
			Debug.LogError("Configure must be called after AddChild.");
			return;
		}
		AssignedAddress = ipString;
		AssignedPort = port;

		Text = "IP: " + AssignedAddress + "                          " + "PORT: " + AssignedPort;
		
	}

    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
	}

}
