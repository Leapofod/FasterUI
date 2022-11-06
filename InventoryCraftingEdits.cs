using MonoMod.Cil;
using System;
using Terraria.ModLoader;

namespace FasterUI;

internal static class InventoryCraftingEdits
{
	internal static void ApplyInventoryCraftEdit()
	{
		IL.Terraria.Main.DrawInventory += CraftScrollFix;
	}

	private static void CraftScrollFix(ILContext il)
	{
		var c = new ILCursor(il);

		// Goes to availableRecipeY[num67] += 6.5f;
		if (!c.TryGotoNext(MoveType.After,
			x => x.MatchLdcR4(6.5f) &&
			x.Next.MatchAdd() && x.Next.Next.MatchStindR4() &&
			x.Previous.MatchLdindR4() &&
			x.Previous.Previous.MatchDup() &&
			x.Previous.Previous.Previous.MatchLdelema<float>() &&
			x.Previous.Previous.Previous.Previous.MatchLdloc(out _) &&
			x.Previous.Previous.Previous.Previous.Previous.MatchLdsfld(out _)))
		{
			ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct Inventory Index");
			return;
		}

		// Applies delta time to scroll speed constant
		c.EmitDelegate<Func<float, float>>(x => x * 60.0f * (float)TimeKeeper.InventoryDeltaTime *
			FasterUI.CraftScrollMultiplier);

		// Goto if (<availableRecipeY[num67] == 0f> && !recFastScroll)
		int loopIndexVal = -1;

		if (!c.TryGotoPrev(MoveType.After,
			x => x.MatchBneUn(out _) &&
			x.Previous.MatchLdcR4(0.0f) &&
			x.Previous.Previous.MatchLdelemR4() &&
			x.Previous.Previous.Previous.MatchLdloc(out loopIndexVal) &&
			x.Previous.Previous.Previous.Previous.MatchLdsfld(out _)))
		{
			ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct Inventory Index");
			return;
		}

		// Label to the availableRecipeY[num67] == 0 check;
		ILLabel jumpFromOr = c.MarkLabel();
		if (!c.TryGotoPrev(MoveType.Before, x => x.MatchLdloc(out _)) ||
			!c.TryGotoPrev(MoveType.Before, x => x.MatchLdsfld(out _)) ||
			loopIndexVal == -1)
		{
			ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct Inventory Index");
			return;
		}

		// Modifies condition to ((availableRecipeY[num67] - TimeKeeperValue < 0f && availableRecipeY[num67] > 0f) || availableRecipeY[num67] == 0f)
		c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, loopIndexVal);
		c.EmitDelegate<Func<int, bool>>
			(loopIndex =>
			Terraria.Main.availableRecipeY[loopIndex] - (6.5f * 60 * (float)TimeKeeper.LastInventoryDeltaTime *
				FasterUI.CraftScrollMultiplier) < 0f &&
			Terraria.Main.availableRecipeY[loopIndex] > 0
			);
		c.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, jumpFromOr);

		// Goes to availableRecipeY[num67] -= 6.5f;
		if (!c.TryGotoNext(MoveType.After,
			x => x.MatchLdcR4(6.5f) &&
			x.Next.MatchSub() && x.Next.Next.MatchStindR4() &&
			x.Previous.MatchLdindR4() &&
			x.Previous.Previous.MatchDup() &&
			x.Previous.Previous.Previous.MatchLdelema<float>() &&
			x.Previous.Previous.Previous.Previous.MatchLdloc(out _) &&
			x.Previous.Previous.Previous.Previous.Previous.MatchLdsfld(out _)))
		{
			ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct Inventory Index 2");
			return;
		}

		// Applies delta time to scroll speed content for other direction
		c.EmitDelegate<Func<float, float>>(x => x * 60.0f * (float)TimeKeeper.InventoryDeltaTime *
			FasterUI.CraftScrollMultiplier);

		// Goto if (<availableRecipeY[num67] == 0f> && !recFastScroll)
		if (!c.TryGotoPrev(MoveType.After,
			x => x.MatchBneUn(out _) &&
			x.Previous.MatchLdcR4(0.0f) &&
			x.Previous.Previous.MatchLdelemR4() &&
			x.Previous.Previous.Previous.MatchLdloc(loopIndexVal) &&
			x.Previous.Previous.Previous.Previous.MatchLdsfld(out _)))
		{
			ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct Inventory Index");
			return;
		}

		// Label to the availableRecipeY[num67] == 0 check;
		ILLabel jumpFromOr2 = c.MarkLabel();
		if (!c.TryGotoPrev(MoveType.Before, x => x.MatchLdloc(out _)) ||
			!c.TryGotoPrev(MoveType.Before, x => x.MatchLdsfld(out _)) ||
			loopIndexVal == -1)
		{
			ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct Inventory Index");
			return;
		}

		// Modifies condition to ((availableRecipeY[num67] + TimeKeeperValue > 0f && availableRecipeY[num67] < 0f) || availableRecipeY[num67] == 0f)
		c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, loopIndexVal);
		c.EmitDelegate<Func<int, bool>>
			(loopIndex =>
			Terraria.Main.availableRecipeY[loopIndex] + (6.5f * 60 * (float)TimeKeeper.LastInventoryDeltaTime *
				FasterUI.CraftScrollMultiplier) > 0f &&
			Terraria.Main.availableRecipeY[loopIndex] < 0
			);
		c.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, jumpFromOr2);
	}
}
