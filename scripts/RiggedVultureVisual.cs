using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Visual-only <see cref="Node3D"/> that plays a random clip from <see cref="Animations"/>
/// after a random delay between actions.
/// </summary>
/// <remarks>
/// On ready, starts <see cref="AnimationDelayTimer"/>. When the timer fires, a random animation
/// is played on <see cref="AnimationPlayer"/>. When that animation finishes, the delay timer
/// is started again. Requires the scene to connect the timer <c>timeout</c> and player
/// <c>animation_finished</c> signals to the matching private handlers.
/// </remarks>
public partial class RiggedVultureVisual : Node3D
{
	/// <summary>
	/// One-shot timer that waits between animation plays.
	/// </summary>
	[Export] public Timer AnimationDelayTimer { get; set; }

	/// <summary>
	/// Player that runs the clips listed in <see cref="Animations"/>.
	/// </summary>
	[Export] public AnimationPlayer AnimationPlayer { get; set; }

	/// <summary>
	/// Shortest delay in seconds before the next animation may start.
	/// </summary>
	[Export] public float MinTimeBetweenActions { get; set; }

	/// <summary>
	/// Longest delay in seconds before the next animation may start.
	/// </summary>
	[Export] public float MaxTimeBetweenActions { get; set; }

	/// <summary>
	/// Animation names to choose from when the delay timer fires.
	/// </summary>
	[Export] public Array<string> Animations { get; set; }

	/// <summary>
	/// Starts the first inter-action delay on <see cref="AnimationDelayTimer"/>.
	/// </summary>
	public override void _Ready()
	{
		AnimationDelayTimer.Start(CalculateDelay());
	}

	/// <summary>
	/// Returns a random delay in seconds between <see cref="MinTimeBetweenActions"/>
	/// and <see cref="MaxTimeBetweenActions"/> (inclusive via lerp).
	/// </summary>
	private float CalculateDelay()
	{
		float randomFac = GD.Randf();
		return Mathf.Lerp(MinTimeBetweenActions, MaxTimeBetweenActions, randomFac);
	}

	/// <summary>
	/// Plays a random entry from <see cref="Animations"/> when the delay elapses.
	/// </summary>
	/// <remarks>
	/// If <see cref="Animations"/> is empty or <see cref="AnimationPlayer"/> is already playing,
	/// restarts the delay timer instead of starting a new clip.
	/// </remarks>
	private void _on_animation_delay_timer_timeout()
	{
		if (Animations == null || Animations.Count == 0 || AnimationPlayer.IsPlaying())
		{
			AnimationDelayTimer.Start(CalculateDelay());
			return;
		}

		int randomIndex = GD.RandRange(0, Animations.Count - 1);
		AnimationPlayer.Play(Animations[randomIndex]);
	}

	/// <summary>
	/// Restarts <see cref="AnimationDelayTimer"/> after <see cref="AnimationPlayer"/> finishes a clip.
	/// </summary>
	/// <param name="animName">Name of the animation that finished (unused).</param>
	private void _on_animation_player_animation_finished(StringName animName)
	{
		AnimationDelayTimer.Start(CalculateDelay());
	}
}
