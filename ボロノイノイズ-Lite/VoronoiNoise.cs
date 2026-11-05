using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;

namespace YukkuriMovieMaker.Player.Video.Effects.CustomEffects;

internal class VoronoiNoise : NoiseBase
{
	[CustomEffect(1, null, null, null, null)]
	private class EffectImpl : EffectImplBase<EffectImpl>
	{
		[CustomEffectProperty(PropertyType.Float, 0)]
		public override float X
		{
			get
			{
				return base.X;
			}
			set
			{
				base.X = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 1)]
		public override float Y
		{
			get
			{
				return base.Y;
			}
			set
			{
				base.Y = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 2)]
		public override float Z
		{
			get
			{
				return base.Z;
			}
			set
			{
				base.Z = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 3)]
		public override float Threshold
		{
			get
			{
				return base.Threshold;
			}
			set
			{
				base.Threshold = value;
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 10)]
		public override int Levels
		{
			get
			{
				return base.Levels;
			}
			set
			{
				base.Levels = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 4)]
		public override float ScaleX
		{
			get
			{
				return base.ScaleX;
			}
			set
			{
				base.ScaleX = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 5)]
		public override float ScaleY
		{
			get
			{
				return base.ScaleY;
			}
			set
			{
				base.ScaleY = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 6)]
		public override float ScaleZ
		{
			get
			{
				return base.ScaleZ;
			}
			set
			{
				base.ScaleZ = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 7)]
		public override float Strength
		{
			get
			{
				return base.Strength;
			}
			set
			{
				base.Strength = value;
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 8)]
		public override int Octaves
		{
			get
			{
				return base.Octaves;
			}
			set
			{
				base.Octaves = value;
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 9)]
		public override int Seed
		{
			get
			{
				return base.Seed;
			}
			set
			{
				base.Seed = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 11)]
		public override float Angle
		{
			get
			{
				return base.Angle;
			}
			set
			{
				base.Angle = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 12)]
		public override float WarpStrength
		{
			get
			{
				return base.WarpStrength;
			}
			set
			{
				base.WarpStrength = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 13)]
		public override float WarpScale
		{
			get
			{
				return base.WarpScale;
			}
			set
			{
				base.WarpScale = value;
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 14)]
		public override int FractalMode
		{
			get
			{
				return base.FractalMode;
			}
			set
			{
				base.FractalMode = value;
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 15)]
		public override int DistanceMode
		{
			get
			{
				return base.DistanceMode;
			}
			set
			{
				base.DistanceMode = value;
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 16)]
		public override int CellOutput
		{
			get
			{
				return base.CellOutput;
			}
			set
			{
				base.CellOutput = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 17)]
		public override float Lacunarity
		{
			get
			{
				return base.Lacunarity;
			}
			set
			{
				base.Lacunarity = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 18)]
		public override float Gain
		{
			get
			{
				return base.Gain;
			}
			set
			{
				base.Gain = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 19)]
		public override float StripeInterval
		{
			get
			{
				return base.StripeInterval;
			}
			set
			{
				base.StripeInterval = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 20)]
		public override float StripeWarpAmount
		{
			get
			{
				return base.StripeWarpAmount;
			}
			set
			{
				base.StripeWarpAmount = value;
			}
		}

		[CustomEffectProperty(PropertyType.Float, 21)]
		public override float FlowAmount
		{
			get
			{
				return base.FlowAmount;
			}
			set
			{
				base.FlowAmount = value;
			}
		}

		public EffectImpl()
			: base("VoronoiNoise")
		{
		}
	}

	public VoronoiNoise(IGraphicsDevicesAndContext devices)
		: base(devices, D2D1CustomShaderEffectBase.Create<EffectImpl>(devices))
	{
	}
}
