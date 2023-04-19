using Sandbox;
using Editor;
using System.Linq;

namespace Conna.Time;

public abstract class TimeGradient<T>
{
	private struct GradientNode
	{
		public T Value;
		public float Time;

		public GradientNode( T value, float time )
		{
			Value = value;
			Time = time;
		}
	}

	private GradientNode[] Nodes;

	public void SetValues( T dawn, T day, T dusk, T night )
	{
		Nodes = new GradientNode[7];
		Nodes[0] = new GradientNode( night, 0f );
		Nodes[1] = new GradientNode( night, 0.2f );
		Nodes[2] = new GradientNode( dawn, 0.35f );
		Nodes[3] = new GradientNode( day, 0.5f );
		Nodes[4] = new GradientNode( day, 0.7f );
		Nodes[5] = new GradientNode( dusk, 0.85f );
		Nodes[6] = new GradientNode( night, 1f );
	}

	public abstract T Interpolate( T a, T b, float fraction );

	public T Evaluate( float fraction )
	{
		for ( var i = 0; i < Nodes.Length; i++ )
		{
			var node = Nodes[i];
			var nextIndex = i + 1;

			if ( Nodes.Length < nextIndex )
				nextIndex = 0;

			var nextNode = Nodes[nextIndex];

			if ( fraction >= node.Time && fraction <= nextNode.Time )
			{
				var duration = (nextNode.Time - node.Time);
				var interpolate = (1f / duration) * (fraction - node.Time);
				return Interpolate( node.Value, nextNode.Value, interpolate );
			}
		}

		return Nodes[0].Value;
	}
}

public class ColorGradient : TimeGradient<Color>
{
	public override Color Interpolate( Color a, Color b, float fraction )
	{
		return Color.Lerp( a, b, fraction );
	}
}

public class FloatGradient : TimeGradient<float>
{
	public override float Interpolate( float a, float b, float fraction )
	{
		return a.LerpTo( b, fraction );
	}
}

[Title( "Time Controller" )]
[EditorSprite( "materials/editor/time_controller.vmat" )]
[Description( "Controls the day and night cycle in the game and defines the colors to blend between." )]
[Category( "Controllers" )]
[HammerEntity]
public partial class TimeController : ModelEntity
{
	[Property( Title = "Dawn Color" )]
	[DefaultValue( "162 118 72" )]
	public Color DawnColor { get; set; }

	[Property( Title = "Dawn Sky Color" )]
	[DefaultValue( "162 118 72" )]
	public Color DawnSkyColor { get; set; }

	[Property( Title = "Day Color" )]
	[DefaultValue( "252 243 222" )]
	public Color DayColor { get; set; }

	[Property( Title = "Day Sky Color" )]
	[DefaultValue( "181 216 229" )]
	public Color DaySkyColor { get; set; }

	[Property( Title = "Dusk Color" )]
	[DefaultValue( "162 118 72" )]
	public Color DuskColor { get; set; }

	[Property( Title = "Dusk Sky Color" )]
	[DefaultValue( "162 118 72" )]
	public Color DuskSkyColor { get; set; }

	[Property( Title = "Night Color" )]
	[DefaultValue( "1 4 18" )]
	public Color NightColor { get; set; }

	[Property( Title = "Night Sky Color" )]
	[DefaultValue( "11 10 21" )]
	public Color NightSkyColor { get; set; }

	[Property( Title = "Dawn Temperature" ), Category( "Temperature" )]
	public float DawnTemperature { get; set; } = 0f;

	[Property( Title = "Day Temperature" ), Category( "Temperature" )]
	public float DayTemperature { get; set; } = 10f;

	[Property( Title = "Dusk Temperature" ), Category( "Temperature" )]
	public float DuskTemperature { get; set; } = 0f;

	[Property( Title = "Night Temperature" ), Category( "Temperature" )]
	public float NightTemperature { get; set; } = -10f;

	[Net, Property, Category( "Fog" )]
	public bool EnableFog { get; set; }

	[Net, Property( Title = "Dawn Fog Color" ), Category( "Fog" )]
	[DefaultValue( "162 118 72" )]
	public Color DawnFogColor { get; set; }

	[Net, Property( Title = "Day Fog Color" ), Category( "Fog" )]
	[DefaultValue( "255 255 255" )]
	public Color DayFogColor { get; set; }

	[Net, Property( Title = "Dusk Fog Color" ), Category( "Fog" )]
	[DefaultValue( "162 118 72" )]
	public Color DuskFogColor { get; set; }

	[Net, Property( Title = "Night Fog Color" ), Category( "Fog" )]
	[DefaultValue( "11 10 21" )]
	public Color NightFogColor { get; set; }

	[Net, Property( Title = "Dawn Fog Start Distance" ), Category( "Fog" )]
	public float DawnFogStart { get; set; } = 30f;

	[Net, Property( Title = "Day Fog Start Distance" ), Category( "Fog" )]
	public float DayFogStart { get; set; }

	[Net, Property( Title = "Dusk Fog Start Distance" ), Category( "Fog" )]
	public float DuskFogStart { get; set; } = 30f;

	[Net, Property( Title = "Night Fog Start Distance" ), Category( "Fog" )]
	public float NightFogStart { get; set; } = 60f;

	protected Output OnBecomeNight { get; set; }
	protected Output OnBecomeDusk { get; set; }
	protected Output OnBecomeDawn { get; set; }
	protected Output OnBecomeDay { get; set; }

	public EnvironmentLightEntity Environment
	{
		get
		{
			InternalEnvironment ??= All.OfType<EnvironmentLightEntity>().FirstOrDefault();
			return InternalEnvironment;
		}
	}

	private EnvironmentLightEntity InternalEnvironment;
	private FloatGradient FogStartGradient;
	private FloatGradient BrightnessGradient;
	private FloatGradient TemperatureGradient;
	private ColorGradient FogColorGradient;
	private ColorGradient SkyColorGradient;
	private ColorGradient ColorGradient;
	private float DefaultSkyIntensity = 1f;
	private float DefaultBrightness = 2.5f;

	public override void ClientSpawn()
	{
		FogStartGradient = new FloatGradient();
		FogStartGradient.SetValues( DawnFogStart, DayFogStart, DuskFogStart, NightFogStart );

		FogColorGradient = new ColorGradient();
		FogColorGradient.SetValues( DawnFogColor, DayFogColor, DuskFogColor, NightFogColor );

		base.ClientSpawn();
	}

	public override void Spawn()
	{
		ColorGradient = new ColorGradient();
		ColorGradient.SetValues( DawnColor, DayColor, DuskColor, NightColor );

		SkyColorGradient = new ColorGradient();
		SkyColorGradient.SetValues( DawnSkyColor, DaySkyColor, DuskSkyColor, NightSkyColor );

		TemperatureGradient = new FloatGradient();
		TemperatureGradient.SetValues( DawnTemperature, DayTemperature, DuskTemperature, NightTemperature );

		BrightnessGradient = new FloatGradient();
		BrightnessGradient.SetValues( 0.75f, 1f, 0.75f, 0.6f );

		TimeSystem.OnSectionChanged += HandleTimeSectionChanged;

		if ( Environment.IsValid() )
		{
			DefaultSkyIntensity = Environment.SkyIntensity;
			DefaultBrightness = Environment.Brightness;
		}

		base.Spawn();
	}

	private void HandleTimeSectionChanged( TimeSection section )
	{
		if ( section == TimeSection.Dawn )
			OnBecomeDawn.Fire( this );
		else if ( section == TimeSection.Day )
			OnBecomeDay.Fire( this );
		else if ( section == TimeSection.Dusk )
			OnBecomeDusk.Fire( this );
		else if ( section == TimeSection.Night )
			OnBecomeNight.Fire( this );
	}

	[Event.Tick.Client]
	private void ClientTick()
	{
		if ( !EnableFog ) return;

		var fraction = (1f / 24f) * TimeSystem.TimeOfDay;

		var fog = Game.SceneWorld.GradientFog;
		fog.StartDistance = FogStartGradient.Evaluate( fraction );
		fog.Color = FogColorGradient.Evaluate( fraction );
		Game.SceneWorld.GradientFog = fog;
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		var environment = Environment;
		if ( !environment.IsValid() ) return;

		var sunAngle = ((TimeSystem.TimeOfDay / 24f) * 360f);
		var radius = 10000f;

		var fraction = (1f / 24f) * TimeSystem.TimeOfDay;

		environment.Color = ColorGradient.Evaluate( fraction );
		environment.SkyColor = SkyColorGradient.Evaluate( fraction );
		environment.Brightness = DefaultBrightness * BrightnessGradient.Evaluate( fraction );
		environment.SkyIntensity = DefaultSkyIntensity * BrightnessGradient.Evaluate( fraction );

		environment.Position = Vector3.Zero + Rotation.From( 0, 0, sunAngle + 60f ) * ( radius * Vector3.Right );
		environment.Position += Rotation.From( 0, sunAngle, 0 ) * ( radius * Vector3.Forward );

		var direction = (Vector3.Zero - environment.Position).Normal;
		environment.Rotation = Rotation.LookAt( direction, Vector3.Up );

		TimeSystem.Temperature = TemperatureGradient.Evaluate( fraction );
	}
}
