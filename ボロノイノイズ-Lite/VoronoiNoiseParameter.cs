using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.ItemEditor;
using YukkuriMovieMaker.Player.Video.Effects.CustomEffects;
using YukkuriMovieMaker.Resources.Localization;

namespace YukkuriMovieMaker.Project.Effects.NoiseParameters;

internal class VoronoiNoiseParameter : BasicNoiseParameterBase
{
	private CellDistanceMode distanceMode;

	private VoronoiOutputMode outputMode;

	[Display(Name = "CellDistanceModeName", Description = "CellDistanceModeDesc", Order = 0, ResourceType = typeof(Texts))]
	[YMM4Only]
	[EnumComboBox]
	public CellDistanceMode DistanceMode
	{
		get
		{
			return distanceMode;
		}
		set
		{
			Set(ref distanceMode, value, "DistanceMode");
		}
	}

	[Display(Name = "VoronoiOutputModeName", Description = "VoronoiOutputModeDesc", Order = 1, ResourceType = typeof(Texts))]
	[YMM4Only]
	[EnumComboBox]
	public VoronoiOutputMode OutputMode
	{
		get
		{
			return outputMode;
		}
		set
		{
			Set(ref outputMode, value, "OutputMode");
		}
	}

	public VoronoiNoiseParameter(SharedDataStore store)
		: base(store)
	{
	}

	[Obsolete("JsonSerializer用")]
	public VoronoiNoiseParameter()
		: this(null)
	{
	}

	public override ID2D1Effect CreateEffect(IGraphicsDevicesAndContext devices)
	{
		VoronoiNoise voronoiNoise = new VoronoiNoise(devices);
		if (!voronoiNoise.IsEnabled)
		{
			voronoiNoise?.Dispose();
			return null;
		}
		voronoiNoise.Initialize();
		return voronoiNoise;
	}

	public override void SetParameterTo(ID2D1Effect effect, int frame, int length, int fps, int seed)
	{
		base.SetParameterTo(effect, frame, length, fps, seed);
		if (effect is NoiseBase noiseBase)
		{
			noiseBase.DistanceMode = (int)DistanceMode;
			noiseBase.CellOutput = (int)OutputMode;
		}
	}

	public override HashCode GetParameterHash(int frame, int length, int fps)
	{
		HashCode parameterHash = base.GetParameterHash(frame, length, fps);
		parameterHash.Add(DistanceMode);
		parameterHash.Add(OutputMode);
		return parameterHash;
	}

	protected override void SaveSharedData(SharedDataStore store)
	{
		base.SaveSharedData(store);
		store.Save(new CellDistanceParameterData(DistanceMode));
		store.Save(new VoronoiNoiseParameterData(this));
	}

	protected override void LoadSharedData(SharedDataStore store)
	{
		base.LoadSharedData(store);
		CellDistanceParameterData cellDistanceParameterData = store.Load<CellDistanceParameterData>();
		if (cellDistanceParameterData != null)
		{
			DistanceMode = cellDistanceParameterData.DistanceMode;
		}
		VoronoiNoiseParameterData voronoiNoiseParameterData = store.Load<VoronoiNoiseParameterData>();
		if (voronoiNoiseParameterData != null)
		{
			OutputMode = voronoiNoiseParameterData.OutputMode;
		}
	}

	public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription, bool isEnabled, int seed)
	{
		return CreateExoVideoFilters(keyFrameIndex, exoOutputDescription, isEnabled, NoiseType.Voronoi, seed);
	}
}
