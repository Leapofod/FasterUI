using MonoMod.Cil;
using System;
using Terraria.ModLoader;

namespace FasterUI;

internal static class QuickStackEdits
{
	private static int stackSplitMultiplier => (int)Math.Max(1, 60 * TimeKeeper.DoDrawDeltaTime);

	internal static void ApplyQuickStackEdits()
	{
		IL.Terraria.Main.DoDraw += (il) =>
		{
			var c = new ILCursor(il);

			#region Unused
			//int local_i_index = -1;

			//// Gets the index for the i local variable for for loops
			//if(!c.TryGotoNext(
			//	x => x.MatchLdcI4(0) &&
			//	x.Next.MatchStloc(out local_i_index) &&
			//	x.Next.Next.MatchBr(out _)) && 
			//	local_i_index == -1)
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}
			#endregion

			// Goes to Main.stackDelay--;
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(1) &&
				x.Previous.MatchLdsfld(out var Main_stackDelay) &&
				x.Next.MatchSub() &&
				x.Next.Next.MatchStsfld(Main_stackDelay)))
			{
				ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
				return;
			}

			// Subtract using time factor as well?
			//c.EmitDelegate<Func<int, int>>(one => Math.Min(Math.Max(1, Terraria.Main.stackSplit - 1), one * stackSplitMultiplier));
			c.EmitDelegate<Func<int, int>>(one => one * stackSplitMultiplier);

			// Goes to Main.stackCounter++;
			if (!c.TryGotoPrev(MoveType.After,
				x => x.Previous.MatchLdsfld(out var Main_stackCounter) &&
				x.MatchLdcI4(1) &&
				x.Next.MatchAdd() &&
				x.Next.Next.MatchStsfld(Main_stackCounter)))
			{
				ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
				return;
			}

			// Multiplies by multiplier
			// Should speed up acceleration??
			//c.EmitDelegate<Func<int, int>>(one => Math.Min(Math.Max(1, Terraria.Main.stackSplit - 1), one * stackSplitMultiplier));
			c.EmitDelegate<Func<int, int>>(one => one * stackSplitMultiplier);

			// Reposition for next jumps
			if (!c.TryGotoNext(x => x.MatchAdd()))
			{
				ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
				return;
			}

			#region Unused
			//// Goes to before stackCounter++;
			//if (!c.TryGotoPrev(MoveType.Before,
			//	x => x.MatchLdsfld(out var Main_stackCounter) &&
			//	x.Next.MatchLdcI4(1) && 
			//	x.Next.Next.MatchAdd() &&
			//	x.Next.Next.Next.MatchStsfld(Main_stackCounter)))
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}

			//var loopBeginningLabel = c.DefineLabel();
			//var loopSkipLabel = c.DefineLabel();
			//var loopEndLabel = c.DefineLabel();

			//// Define a fod loop -> (int i = 0; i < stackSplitModifier - 1; i++)
			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Stloc, local_i_index);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Br_S, loopSkipLabel);
			//c.MarkLabel(loopBeginningLabel);

			//if (!c.TryGotoNext(
			//	x => x.Previous.Previous.MatchLdsfld(out _) &&
			//	x.Previous.MatchLdloc(out _) &&
			//	x.MatchBlt(out _)))
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}
			//c.Emit(Mono.Cecil.Cil.OpCodes.Blt_S, loopEndLabel);
			//c.Remove();

			//Mono.Cecil.FieldReference stackCounter = null;
			//if(!c.TryGotoNext(
			//	x => x.Previous.MatchLdcI4(0) &&
			//	x.MatchStsfld(out stackCounter)))
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}

			//c.Emit(Mono.Cecil.Cil.OpCodes.Stsfld, stackCounter);
			//c.MarkLabel(loopEndLabel);

			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, local_i_index);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Add);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Stloc, local_i_index);
			//c.MarkLabel(loopSkipLabel);

			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, local_i_index);
			//c.EmitDelegate(() => stackSplitMultiplier);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Blt_S, loopBeginningLabel);

			//c.Remove();
			#endregion

			// Goes to Main.mapTime--;
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(1) &&
				x.Previous.MatchLdsfld(out var Main_mapTime) &&
				x.Next.MatchSub() &&
				x.Next.Next.MatchStsfld(Main_mapTime)))
			{
				ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
				return;
			}


			// goes to Main.stackSplit--;
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(1) &&
				x.Previous.MatchLdsfld(out var Main_stackSplit) &&
				x.Next.MatchSub() &&
				x.Next.Next.MatchStsfld(Main_stackSplit)))
			{
				ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
				return;
			}

			// Multiplies by delta time correction, doesn't go below 0, doesn't subtract to 0 immediately
			c.EmitDelegate<Func<int, int>>(one => Math.Min(Math.Max(1, Terraria.Main.stackSplit - 1), one * stackSplitMultiplier));
		};

		IL.Terraria.UI.ItemSlot.HandleShopSlot += il =>
		{
			var c = new ILCursor(il);
			int numIndex = -1;

			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchStloc(out numIndex) && 
				x.Previous.MatchAdd() &&
				x.Previous.Previous.MatchLdcI4(1) &&
				x.Previous.Previous.Previous.MatchLdsfld(out _)) &&	
				numIndex == -1)
			{
				ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct HandleShopSlot Index");
				return;
			}

			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, numIndex);
			c.EmitDelegate<Func<int, int>>(num => num == 1 ? num : num * stackSplitMultiplier);
			c.Emit(Mono.Cecil.Cil.OpCodes.Stloc, numIndex);
		};
	}
}
