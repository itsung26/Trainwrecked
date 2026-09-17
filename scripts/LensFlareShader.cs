using Godot;
using System;

/// <summary>
/// Fullscreen <see cref="ColorRect"/> that drives a lens-flare <see cref="ShaderMaterial"/>
/// from a sun <see cref="DirectionalLight3D"/>.
/// </summary>
/// <remarks>
/// Each frame, approximates the sun's screen position from <see cref="Sun"/>'s orientation,
/// writes it to the material's <c>sun_position</c> parameter, and sets <c>flare_strength</c>
/// when the sun leaves the viewport (does not write <see cref="CanvasItem.Visible"/>).
/// Requires an assigned <see cref="Sun"/>, a <see cref="ShaderMaterial"/> on
/// <see cref="CanvasItem.Material"/>, and an active <see cref="Camera3D"/> in the viewport.
/// </remarks>
public sealed partial class LensFlareShader : ColorRect
{
	/// <summary>
	/// Distant sun light whose orientation defines where the flare appears on screen.
	/// </summary>
	[Export] public DirectionalLight3D Sun;
	private ShaderMaterial _lensFlareShader;

	/// <summary>
	/// Caches <see cref="CanvasItem.Material"/> as a <see cref="ShaderMaterial"/> when this node enters the tree.
	/// </summary>
	public override void _Ready()
	{
		_lensFlareShader = Material as ShaderMaterial;
	}

	/// <summary>
	/// Updates <c>flare_strength</c> and <c>sun_position</c> from <see cref="Sun"/> each frame.
	/// </summary>
	/// <remarks>
	/// No-ops when this node is hidden, or when <see cref="Sun"/> or the cached shader material is missing.
	/// Does not modify <see cref="CanvasItem.Visible"/>; sun gating sets the shader's <c>flare_strength</c>
	/// so callers can still show or hide this node normally.
	/// </remarks>
	/// <param name="delta">Frame time in seconds (unused).</param>
	public override void _Process(double delta)
	{
		if (!Visible || Sun is null || _lensFlareShader is null)
		{
			return;
		}

		if (!IsSunVisible())
		{
			SetFlareStrength(0.0f);
			return;
		}

		SetFlareStrength(1.0f);
		UpdateShaderSunPosition(GetSunPositionOnScreen());
	}

	/// <summary>
	/// Returns the sun's screen-space (pixel) position on the viewport.
	/// </summary>
	/// <remarks>
	/// Directional lights have no finite world position, so this samples a point along
	/// <see cref="Sun"/>'s sky direction (<c>+GlobalBasis.Z</c>, opposite light emission)
	/// and unprojects it through the active camera.
	/// </remarks>
	/// <returns>
	/// Pixel coordinates suitable for the shader's <c>sun_position</c> uniform,
	/// or <see cref="Vector2.Zero"/> when there is no camera, no <see cref="Sun"/>,
	/// or the sample lies behind the camera.
	/// </returns>
	private Vector2 GetSunPositionOnScreen()
	{
		Camera3D camera = GetViewport()?.GetCamera3D();
		if (camera == null || Sun == null)
		{
			return Vector2.Zero;
		}

		// Light emits along -Z; the sun appears in the opposite direction (+Z).
		Vector3 samplePosition = camera.GlobalPosition + Sun.GlobalBasis.Z;
		if (camera.IsPositionBehind(samplePosition))
		{
			return Vector2.Zero;
		}

		return camera.UnprojectPosition(samplePosition);
	}

	/// <summary>
	/// Writes <paramref name="posOnScren"/> to the material's <c>sun_position</c> shader parameter.
	/// </summary>
	/// <param name="posOnScren">Screen-space pixel position of the sun.</param>
	private void UpdateShaderSunPosition(Vector2 posOnScren)
	{
		_lensFlareShader.SetShaderParameter("sun_position", posOnScren);
	}

	/// <summary>
	/// Writes <paramref name="strength"/> to the material's <c>flare_strength</c> shader parameter.
	/// </summary>
	/// <param name="strength">
	/// <c>0</c> makes the overlay fully transparent; <c>1</c> draws the full flare.
	/// </param>
	private void SetFlareStrength(float strength)
	{
		_lensFlareShader.SetShaderParameter("flare_strength", strength);
	}

	private bool IsSunVisible()
	{
		return IsDirectionalLightOnViewport(Sun);
	}

	/// <summary>
	/// Returns whether <paramref name="light"/>'s sky direction projects onto the active camera's viewport.
	/// </summary>
	/// <param name="light">Directional light treated as an infinitely distant celestial body.</param>
	/// <returns>
	/// <see langword="true"/> when <paramref name="light"/> is non-null and visible, a camera exists,
	/// the sample point is in front of the camera, and its unprojected position lies inside
	/// <see cref="Viewport.GetVisibleRect"/>; otherwise <see langword="false"/>.
	/// </returns>
	private bool IsDirectionalLightOnViewport(DirectionalLight3D light)
	{
		if (light == null || !light.Visible)
		{
			return false;
		}

		Camera3D camera = GetViewport()?.GetCamera3D();
		if (camera == null)
		{
			return false;
		}

		// Light emits along -Z; the celestial body appears in the opposite direction (+Z).
		Vector3 samplePosition = camera.GlobalPosition + light.GlobalBasis.Z;
		if (camera.IsPositionBehind(samplePosition))
		{
			return false;
		}

		Vector2 screenPosition = camera.UnprojectPosition(samplePosition);
		Rect2 viewportRect = GetViewport().GetVisibleRect();
		return viewportRect.HasPoint(screenPosition);
	}
}
