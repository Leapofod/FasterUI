using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;

namespace FasterUI;

internal class TimeKeeper : ModSystem
{
	/// <summary>
	/// Amount of time in seconds between the last frame and current frame
	/// </summary>
	internal static double InventoryDeltaTime => m_inventoryDeltaTime / 1000.0;

	/// <summary>
	/// Previous value of Inventory Delta Time
	/// </summary>
	internal static double LastInventoryDeltaTime => m_lastInventoryDeltaTime / 1000.0;

	private static long m_lastInventoryDeltaTime;
	private static long m_inventoryDeltaTime;

	private static long m_lastInventoryUnixTimeMilli = DateTimeOffset.Now.ToUnixTimeMilliseconds();

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int invIdx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
		if (invIdx > -1)
		{
			layers.Insert(invIdx, new LegacyGameInterfaceLayer("FasterUI: Inventory Time Keeper", delegate
			{
				m_lastInventoryDeltaTime = m_inventoryDeltaTime;

				var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				m_inventoryDeltaTime = now - m_lastInventoryUnixTimeMilli;
				m_lastInventoryUnixTimeMilli = now;
				
				return true;
			}, InterfaceScaleType.None));
		}
	}

}
