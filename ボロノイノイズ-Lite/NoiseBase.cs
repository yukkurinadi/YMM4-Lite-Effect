using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;

namespace YukkuriMovieMaker.Player.Video.Effects.CustomEffects;

internal class NoiseBase(IGraphicsDevicesAndContext devices, nint ptr) : D2D1CustomShaderEffectBase(ptr)
{
	protected abstract class EffectImplBase<U> : D2D1CustomShaderEffectImplBase<EffectImplBase<U>> where U : EffectImplBase<U>
	{
		protected ConstantBuffer constants;

		[CustomEffectProperty(PropertyType.Float, 0)]
		public virtual float X
		{
			get
			{
				return constants.X;
			}
			set
			{
				constants.X = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 1)]
		public virtual float Y
		{
			get
			{
				return constants.Y;
			}
			set
			{
				constants.Y = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 2)]
		public virtual float Z
		{
			get
			{
				return constants.Z;
			}
			set
			{
				constants.Z = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 3)]
		public virtual float Threshold
		{
			get
			{
				return constants.Threshold;
			}
			set
			{
				constants.Threshold = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 10)]
		public virtual int Levels
		{
			get
			{
				return constants.Levels;
			}
			set
			{
				constants.Levels = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 4)]
		public virtual float ScaleX
		{
			get
			{
				return constants.ScaleX;
			}
			set
			{
				constants.ScaleX = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 5)]
		public virtual float ScaleY
		{
			get
			{
				return constants.ScaleY;
			}
			set
			{
				constants.ScaleY = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 6)]
		public virtual float ScaleZ
		{
			get
			{
				return constants.ScaleZ;
			}
			set
			{
				constants.ScaleZ = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 7)]
		public virtual float Strength
		{
			get
			{
				return constants.Strength;
			}
			set
			{
				constants.Strength = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 8)]
		public virtual int Octaves
		{
			get
			{
				return constants.Octaves;
			}
			set
			{
				constants.Octaves = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 9)]
		public virtual int Seed
		{
			get
			{
				return constants.Seed;
			}
			set
			{
				constants.Seed = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 11)]
		public virtual float Angle
		{
			get
			{
				return constants.Angle;
			}
			set
			{
				constants.Angle = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 12)]
		public virtual float WarpStrength
		{
			get
			{
				return constants.WarpStrength;
			}
			set
			{
				constants.WarpStrength = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 13)]
		public virtual float WarpScale
		{
			get
			{
				return constants.WarpScale;
			}
			set
			{
				constants.WarpScale = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 14)]
		public virtual int FractalMode
		{
			get
			{
				return constants.FractalMode;
			}
			set
			{
				constants.FractalMode = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 15)]
		public virtual int DistanceMode
		{
			get
			{
				return constants.DistanceMode;
			}
			set
			{
				constants.DistanceMode = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Int32, 16)]
		public virtual int CellOutput
		{
			get
			{
				return constants.CellOutput;
			}
			set
			{
				constants.CellOutput = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 17)]
		public virtual float Lacunarity
		{
			get
			{
				return constants.Lacunarity;
			}
			set
			{
				constants.Lacunarity = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 18)]
		public virtual float Gain
		{
			get
			{
				return constants.Gain;
			}
			set
			{
				constants.Gain = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 19)]
		public virtual float StripeInterval
		{
			get
			{
				return constants.StripeInterval;
			}
			set
			{
				constants.StripeInterval = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 20)]
		public virtual float StripeWarpAmount
		{
			get
			{
				return constants.StripeWarpAmount;
			}
			set
			{
				constants.StripeWarpAmount = value;
				UpdateConstants();
			}
		}

		[CustomEffectProperty(PropertyType.Float, 21)]
		public virtual float FlowAmount
		{
			get
			{
				return constants.FlowAmount;
			}
			set
			{
				constants.FlowAmount = value;
				UpdateConstants();
			}
		}

		protected EffectImplBase(string shaderName)
			: base(ShaderResourceUri.Get(shaderName))
		{
		}

		protected override void UpdateConstants()
		{
			drawInformation?.SetPixelShaderConstantBuffer(in constants);
		}
	}

	protected struct ConstantBuffer
	{
		public float X;

		public float Y;

		public float Z;

		public float Threshold;

		public float ScaleX;

		public float ScaleY;

		public float ScaleZ;

		public float Strength;

		public int Octaves;

		public int Seed;

		public int Levels;

		public float Angle;

		public float WarpStrength;

		public float WarpScale;

		public int FractalMode;

		public int DistanceMode;

		public int CellOutput;

		public float Lacunarity;

		public float Gain;

		public float Padding2;

		public float StripeInterval;

		public float StripeWarpAmount;

		public float FlowAmount;

		public float Padding3;
	}

	protected enum Properties
	{
		X,
		Y,
		Z,
		Threshold,
		ScaleX,
		ScaleY,
		ScaleZ,
		Strength,
		Octaves,
		Seed,
		Levels,
		Angle,
		WarpStrength,
		WarpScale,
		FractalMode,
		DistanceMode,
		CellOutput,
		Lacunarity,
		Gain,
		StripeInterval,
		StripeWarpAmount,
		FlowAmount
	}

	private readonly Flood whiteEffect = new Flood(devices.DeviceContext);

	public float X
	{
		get
		{
			return GetFloatValue(0);
		}
		set
		{
			SetValue(0, value);
		}
	}

	public float Y
	{
		get
		{
			return GetFloatValue(1);
		}
		set
		{
			SetValue(1, value);
		}
	}

	public float Z
	{
		get
		{
			return GetFloatValue(2);
		}
		set
		{
			SetValue(2, value);
		}
	}

	public float Threshold
	{
		get
		{
			return GetFloatValue(3);
		}
		set
		{
			SetValue(3, value);
		}
	}

	public int Levels
	{
		get
		{
			return GetIntValue(10);
		}
		set
		{
			SetValue(10, value);
		}
	}

	public float ScaleX
	{
		get
		{
			return GetFloatValue(4);
		}
		set
		{
			SetValue(4, value);
		}
	}

	public float ScaleY
	{
		get
		{
			return GetFloatValue(5);
		}
		set
		{
			SetValue(5, value);
		}
	}

	public float ScaleZ
	{
		get
		{
			return GetFloatValue(6);
		}
		set
		{
			SetValue(6, value);
		}
	}

	public float Strength
	{
		get
		{
			return GetFloatValue(7);
		}
		set
		{
			SetValue(7, value);
		}
	}

	public int Octaves
	{
		get
		{
			return GetIntValue(8);
		}
		set
		{
			SetValue(8, value);
		}
	}

	public int Seed
	{
		get
		{
			return GetIntValue(9);
		}
		set
		{
			SetValue(9, value);
		}
	}

	public float Angle
	{
		get
		{
			return GetFloatValue(11);
		}
		set
		{
			SetValue(11, value);
		}
	}

	public float WarpStrength
	{
		get
		{
			return GetFloatValue(12);
		}
		set
		{
			SetValue(12, value);
		}
	}

	public float WarpScale
	{
		get
		{
			return GetFloatValue(13);
		}
		set
		{
			SetValue(13, value);
		}
	}

	public int FractalMode
	{
		get
		{
			return GetIntValue(14);
		}
		set
		{
			SetValue(14, value);
		}
	}

	public int DistanceMode
	{
		get
		{
			return GetIntValue(15);
		}
		set
		{
			SetValue(15, value);
		}
	}

	public int CellOutput
	{
		get
		{
			return GetIntValue(16);
		}
		set
		{
			SetValue(16, value);
		}
	}

	public float Lacunarity
	{
		get
		{
			return GetFloatValue(17);
		}
		set
		{
			SetValue(17, value);
		}
	}

	public float Gain
	{
		get
		{
			return GetFloatValue(18);
		}
		set
		{
			SetValue(18, value);
		}
	}

	public float StripeInterval
	{
		get
		{
			return GetFloatValue(19);
		}
		set
		{
			SetValue(19, value);
		}
	}

	public float StripeWarpAmount
	{
		get
		{
			return GetFloatValue(20);
		}
		set
		{
			SetValue(20, value);
		}
	}

	public float FlowAmount
	{
		get
		{
			return GetFloatValue(21);
		}
		set
		{
			SetValue(21, value);
		}
	}

	public void Initialize()
	{
		using ID2D1Image input = whiteEffect.Output;
		SetInput(0, input, true);
	}

	protected override void DisposeCore(nint nativePointer, bool disposing)
	{
		SetInput(0, null, true);
		whiteEffect.Dispose();
		base.DisposeCore(nativePointer, disposing);
	}
}
