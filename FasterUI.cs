using Terraria.ModLoader;

namespace FasterUI;

public class FasterUI : Mod
{
	internal static float CraftScrollMultiplier => ModContent.GetInstance<FasterUIConfig>().CraftingScrollMultiplier;

	public override void Load()
	{
		InventoryCraftingEdits.ApplyInventoryCraftEdit();
		QuickStackEdits.ApplyQuickStackEdits();
	}
}
