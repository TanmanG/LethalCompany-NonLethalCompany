using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace NonLethalCompany
{
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin
	{
		public static Plugin Instance;
		private void Awake()
		{
			Instance = this;
			Logger.LogInfo($"Patch applying...");
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Patch applied!");
		}
		
		[HarmonyPatch(typeof(ForestGiantAI), "Start")]
		[HarmonyPostfix]
		private static void ForestGiantAnimationSoftPatch(ForestGiantAI __instance)
		{
			// Get the clip for the giant eating
			AnimationClip clip = __instance.creatureAnimator.runtimeAnimatorController.animationClips.First(clip => clip.name.Equals("FGiantEatPlayer"));
			// Remove the particles
			clip.events = clip.events.Where(animEvent => !animEvent.functionName.Equals("PlayParticle")).ToArray();
		}
		
		[HarmonyPatch(typeof(ForestGiantAI), "EatPlayerAnimation", MethodType.Enumerator)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> ForestGiantSoftPatch(IEnumerable<CodeInstruction> instructions)
		{
			CodeMatcher matcher = new(instructions);
			
			// Patch the bloody face decal on the forest giant
			matcher.MatchForward(false, 
			                     new CodeMatch(OpCodes.Ldloc_1),
			                     new CodeMatch(OpCodes.Ldfld,
				                     AccessTools.Field(typeof(ForestGiantAI), "bloodOnFaceDecal")),
			                     new CodeMatch(OpCodes.Ldc_I4_1),
			                     new CodeMatch(OpCodes.Callvirt, 
			                                   AccessTools.Method(typeof(Behaviour), "set_enabled")))
			       .RemoveInstructions(4);
			
			return matcher.InstructionEnumeration();
		}


		[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnDeadBody))]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> SpawnBodiesSoftPatch(IEnumerable<CodeInstruction> instructions)
		{
			CodeMatcher matcher = new(instructions);

			matcher.MatchForward(true,
			                     new CodeMatch(OpCodes.Ldc_R4),
			                     new CodeMatch(OpCodes.Stloc_0))
			       .InsertAndAdvance(
				        new CodeInstruction(OpCodes.Ldc_I4, 0),
				        new CodeInstruction(OpCodes.Starg, 5));
			
			// Patch body blood decals
			matcher.MatchForward(false,
			                     new CodeMatch(OpCodes.Ldarg_0),
			                     new CodeMatch(OpCodes.Ldfld,
				                     AccessTools.Field(typeof(PlayerControllerB), "bodyBloodDecals")),
			                     new CodeMatch(OpCodes.Ldlen),
			                     new CodeMatch(OpCodes.Conv_I4))
			       .Advance(-1) // Move back to the start of the for loop condition
			       .SetAndAdvance(OpCodes.Nop, null) // Clear the first variable while preserving the label
			       .RemoveInstructions(5); // Clear out everything else

			return matcher.InstructionEnumeration();
		}
		
		[HarmonyPatch(typeof(PlayerControllerB), "UpdatePlayerPositionClientRpc")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UpdatePositionSoftPatch(IEnumerable<CodeInstruction> instructions)
		{
			CodeMatcher matcher = new(instructions);
			matcher.MatchForward(false,
			                     new CodeMatch(OpCodes.Ldarg_0),
			                     new CodeMatch(OpCodes.Call),
			                     new CodeMatch(OpCodes.Ldarg_0),
			                     new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), "bleedingHeavily")),
			                     new CodeMatch(OpCodes.Ldloc_0),
			                     new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.DropBlood))))
			       .SetAndAdvance(OpCodes.Nop, null) // Clear the first variable while preserving the label
			       .RemoveInstructions(5); // Clear out everything else

			return matcher.InstructionEnumeration();
		}
		
		[HarmonyPatch(typeof(PlayerControllerB), "DamagePlayerFromOtherClientClientRpc")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> DamageRPCSoftPatch(IEnumerable<CodeInstruction> instructions)
		{
			CodeMatcher matcher = new(instructions);
			matcher.MatchForward(false,
			                     new CodeMatch(OpCodes.Ldarg_0),
			                     new CodeMatch(OpCodes.Ldarg_2),
				        new CodeMatch(OpCodes.Ldc_I4_1),
				        new CodeMatch(OpCodes.Ldc_I4_0),
				        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PlayerControllerB), "DropBlood")))
			       .RemoveInstructions(36); // Clear out all gore and sound effects

			return matcher.InstructionEnumeration();
		}
		
		[HarmonyPatch(typeof(PlayerControllerB), "DropBlood")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> DropBloodHardPatch(IEnumerable<CodeInstruction> instructions)
		{
			CodeMatcher matcher = new(instructions);

			// Safety patch to make drop blood do nothing
			matcher.MatchForward(false,
			                     new CodeMatch(OpCodes.Ldc_I4_0),
			                     new CodeMatch(OpCodes.Stloc_0))
			       .SetAndAdvance(OpCodes.Ret, null);

			return matcher.InstructionEnumeration();
		}
		
		[HarmonyPatch(typeof(PlayerControllerB), "AddBloodToBody")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> AddBloodToBodyHardPatch(IEnumerable<CodeInstruction> instructions)
		{
			CodeMatcher matcher = new(instructions);

			// Safety patch to make drop blood do nothing
			matcher.MatchForward(false,
			                     new CodeMatch(OpCodes.Ldc_I4_0),
			                     new CodeMatch(OpCodes.Stloc_0))
			       .SetAndAdvance(OpCodes.Ret, null);

			return matcher.InstructionEnumeration();
		}
		
		[HarmonyPatch(typeof(DeadBodyInfo), "MakeCorpseBloody")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> MakeCorpseBloodyHardPatch(IEnumerable<CodeInstruction> instructions)
		{
			CodeMatcher matcher = new(instructions);

			// Safety patch to make drop blood do nothing
			matcher.MatchForward(false,
			                     new CodeMatch(OpCodes.Ldc_I4_0),
			                     new CodeMatch(OpCodes.Stloc_0))
			       .SetAndAdvance(OpCodes.Ret, null);

			return matcher.InstructionEnumeration();
		}

	}
}
