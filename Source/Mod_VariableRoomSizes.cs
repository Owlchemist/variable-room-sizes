using Verse;
using UnityEngine;
using System.Collections.Generic;
using static VariableRoomSizes.ModSettings_VariableRoomSizes;
 
namespace VariableRoomSizes
{
    public class Mod_VariableRoomSizes : Mod
	{
		public Mod_VariableRoomSizes(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_VariableRoomSizes>();
		}

		
		public override void DoSettingsWindowContents(Rect inRect)
		{
			//=========Reset button=========
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);
			if (options.ButtonText("reset"))
			{
				customMultipliers = null;
				Setup.Reset();
			}
			options.End();

			//=========Body=========
			Rect scrollViewRect = inRect;
			scrollViewRect.y += 35f;
			scrollViewRect.yMax -= 35f;
			Widgets.BeginScrollView(scrollViewRect, ref scrollPos, optionsRect, true);
			options = new Listing_Standard();
			options.Begin(optionsRect);

			int lineCount = 0;

			var list = DefDatabase<RoomRoleDef>.AllDefsListForReading;
			var length = list.Count;
			string[] bufferString = new string[DefDatabase<RoomRoleDef>.DefCount];
			//=========Labels========
			for (int i = 0; i < length; i++)
			{
				var def = list[i];
				Rect row = new Rect(options.curX, lineCount++ * 32f, inRect.width, 32f);
				if (lineCount % 2 != 0) Widgets.DrawLightHighlight(row);
				Widgets.DrawHighlightIfMouseover(row);

				//options.Label(, tooltip: def.label );
				customMultipliersCache[i] = options.SliderLabeled(def.LabelCap + " : " + customMultipliersCache[i], (float)System.Math.Round(customMultipliersCache[i], 1), 0.05f, 6f);
			}
			
			//=========Sliders========
			/*
			options.NewColumn();
			
			for (int i = 0; i < length; i++)
			{
				var def = list[i];
				
				bufferString[i] = customMultipliersCache[i].ToString();
				customMultipliersCache[i] = options.Slider((float)System.Math.Round(customMultipliersCache[i], 1), 0.05f, 6f);
			}
			*/
			
			optionsRect.height = (lineCount + 2) * 32f;
			optionsRect.width = inRect.width - GUI.skin.verticalScrollbar.fixedWidth - 5f;
			options.End();
			Widgets.EndScrollView();
		}

		public override string SettingsCategory()
		{
			return "Variable Room Sizes";
		}

		public override void WriteSettings()
		{
			var list = DefDatabase<RoomRoleDef>.AllDefsListForReading;
            var length = list.Count;
			try
			{
				for (int i = 0; i < length; i++)
				{
					var roomRoleDef = list[i];
					var modX = roomRoleDef.GetModExtension<Size>();
					float defaultValue = modX == null ? 1f : modX.multiplier;
					float customValue = customMultipliersCache[i];
					if (customValue != defaultValue)
					{
						//Add to player settings' data
						if (!customMultipliers.ContainsKey(roomRoleDef.defName)) customMultipliers.Add(roomRoleDef.defName, customValue);
						else customMultipliers[roomRoleDef.defName] = customValue;
						
						Patch_RoomStatWorker_Space.multiplierCache[roomRoleDef] = customValue;
					}
					else
					{
						customMultipliers.Remove(roomRoleDef.defName);
						Patch_RoomStatWorker_Space.multiplierCache[roomRoleDef] = defaultValue;
					}
				}

				if (Current.ProgramState == ProgramState.Playing)
				{
					foreach (var map in Find.Maps)
					{
						map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
					}
				}
			}
			catch (System.Exception ex)
			{
				Log.Error("[Variable Room Sizes] Error updating mod options:\n" + ex);
			}
			

			base.WriteSettings();
		}
		
	}

	public class ModSettings_VariableRoomSizes : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Collections.Look(ref customMultipliers, "customMultipliers", LookMode.Value);
			base.ExposeData();
		}

		public static Dictionary<string, float> customMultipliers;
		public static float[] customMultipliersCache;
		public static Vector2 scrollPos;
		public static Rect optionsRect;
	}
}