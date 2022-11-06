using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace FasterUI;

[Label("$Mods.FasterUI.ConfigTitle")]
internal class FasterUIConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	[Label("$Mods.FasterUI.CraftingScrollMultiplier")]
	[DefaultValue(1.0f)]
	[Range(0.25f, 5f)]
	[Increment(0.25f)]
	[DrawTicks]
	[Slider]
	public float CraftingScrollMultiplier;
}