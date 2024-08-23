using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

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

		[HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.Start))]
		[HarmonyPostfix]
		private static void ManEaterBloodParticlesSoftPatch(CaveDwellerAI __instance)
		{
			ParticleSystem.MainModule playerKillParticleA = __instance.killPlayerParticle1.main;
			playerKillParticleA.maxParticles = 0;
			playerKillParticleA.duration = 0;
			playerKillParticleA.startSize = new ParticleSystem.MinMaxCurve(0);
            
			ParticleSystem.MainModule playerKillParticleB = __instance.killPlayerParticle2.main;
			playerKillParticleB.maxParticles = 0;
			playerKillParticleB.duration = 0;
			playerKillParticleB.startSize = new ParticleSystem.MinMaxCurve(0);
		}
		
		[HarmonyPatch(typeof(KillLocalPlayer), nameof(KillLocalPlayer.KillPlayer))]
		[HarmonyPrefix]
		private static bool FanRoomDecapitateSoftPatch(KillLocalPlayer __instance)
		{
			if (__instance.causeOfDeath != CauseOfDeath.Fan)
				return true;
			
			// Set the death animation to not be decapitation.
			__instance.deathAnimation = 0;
			__instance.causeOfDeath = CauseOfDeath.Fan;
			
			// Check if we already have a spike prefab.
			if (_spikePrefab != null)
			{
				__instance.spawnPrefab = _spikePrefab;
				return true;
			}
			
			// Patch the spike trap attach object to not have blood particles.
			GameObject bodyStickingPoint = Instantiate(__instance.spawnPrefab);
			bodyStickingPoint.GetComponentsInChildren<ParticleSystem>()
			                 .ToList()
			                 .ForEach(particleSystem =>
            {
	            
	            ParticleSystem.MainModule mainModule = particleSystem.main;
	            mainModule.maxParticles = 0;
	            mainModule.duration = 0;
	            mainModule.startSize = new ParticleSystem.MinMaxCurve(0);
				Destroy(particleSystem);
            });
			// Disable the audio on the spike trap.
			bodyStickingPoint.GetComponentsInChildren<AudioSource>().ToList().ForEach(audioSource =>
			{
				audioSource.enabled = false;
			});
			// Mark the spike trap as DontDestroyOnLoad.
			DontDestroyOnLoad(bodyStickingPoint);
			// Send the spike prefab to the void.
			bodyStickingPoint.transform.position += Vector3.down * 10000;
			// Update the spike prefab for use in spikes and fans.
			_spikePrefab = bodyStickingPoint;
			// Update the spawn prefab on the fan.
			__instance.spawnPrefab = bodyStickingPoint;
			
			return true;
		}
		
		[HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.Start))]
		[HarmonyPostfix]
		private static void ButlerBloodParticleSoftPatch(ButlerEnemyAI __instance)
		{
			// Remove the particles from the butler's particle emitters.
			
			ParticleSystem.MainModule stabParticleSettings = __instance.stabBloodParticle.main;
			stabParticleSettings.maxParticles = 0;
			stabParticleSettings.duration = 0;
			stabParticleSettings.startSize = new ParticleSystem.MinMaxCurve(0);
			
			ParticleSystem.MainModule popParticleSettings = __instance.popParticle.main;
			popParticleSettings.maxParticles = 0;
			popParticleSettings.duration = 0;
			popParticleSettings.startSize = new ParticleSystem.MinMaxCurve(0);
		}
		[HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.StabPlayerClientRpc))]
		[HarmonyPostfix]
		private static void ButlerStabParticleSoftPatch(ButlerEnemyAI __instance)
		{
			// Abort the particles from the butler blood particle emitter.
			__instance.stabBloodParticle.Stop();
			__instance.stabBloodParticle.Clear();
		}
		[HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.KillEnemy))]
		[HarmonyPostfix]
		private static void ButlerPopParticleSoftPatch(ButlerEnemyAI __instance)
		{
			// Abort the particles from the butler pop particle emitter.
			__instance.popParticle.Stop();
			__instance.popParticle.Clear();
		}
		
		[HarmonyPatch(typeof(KnifeItem), "ItemActivate")]
		[HarmonyPostfix]
		private static void KnifeBloodParticleSoftPatch(KnifeItem __instance)
		{
			// Remove the particles from the knife particle emitter.
			ParticleSystem.MainModule mainModule = __instance.bloodParticle.main;
			mainModule.maxParticles = 0;
			mainModule.duration = 0;
			mainModule.startSize = new ParticleSystem.MinMaxCurve(0);
		}

		private static GameObject _spikePrefab;
		private static bool _isFirst = true;
		[HarmonyPatch(typeof(SpikeRoofTrap), "Start")]
		[HarmonyPostfix]
		private static void SpikeTrapBloodParticleSoftPatch(SpikeRoofTrap __instance)
		{
			// Check if we already have a spike prefab.
			if (_spikePrefab != null)
			{
				__instance.deadBodyStickingPointPrefab = _spikePrefab;
				return;
			}
			
			// Patch the spike trap attach object to not have blood particles.
			GameObject bodyStickingPoint = Instantiate(__instance.deadBodyStickingPointPrefab);
			bodyStickingPoint.GetComponentsInChildren<ParticleSystem>()
			                 .ToList()
			                 .ForEach(particleSystem =>
			                  {
				                  ParticleSystem.MainModule mainModule = particleSystem.main;
				                  mainModule.maxParticles = 0;
				                  mainModule.duration = 0;
				                  mainModule.startSize = new ParticleSystem.MinMaxCurve(0);
				                  Destroy(particleSystem);
			                  });
			// Disable the audio on the spike trap.
			bodyStickingPoint.GetComponentsInChildren<AudioSource>().ToList().ForEach(audioSource =>
			{
				audioSource.enabled = false;
			});
			// Mark the spike trap as DontDestroyOnLoad.
			DontDestroyOnLoad(bodyStickingPoint);
			// Send the spike prefab to the void.
			bodyStickingPoint.transform.position += Vector3.down * 10000;
			// Update the spike prefab for use in spikes and fans.
			_spikePrefab = bodyStickingPoint;
			// Update the spawn prefab on the fan.
			__instance.deadBodyStickingPointPrefab = bodyStickingPoint;
			
			return;
			// Todo: Look for way to bypass RW lock on mesh.
			if (_isFirst == false)
				return;
			
			Transform parentTransform = __instance.transform.parent;
			for (int i = 0; i < parentTransform.childCount; i++)
			{
				Transform child = parentTransform.GetChild(i);
				if (child.name.Contains("SpikeRoof") == false)
					continue;
					
				// Get the vertices.
				Vector3[] vertices = child.GetComponent<MeshFilter>().mesh.vertices;
						
				// Get the lowest vertex.
				Instance.Logger.LogFatal($"Lowest Y: {vertices.Min(vertex => vertex.y)}");
						
				// Group the vertices, sort them by Y value, then log how many are at each Y-level.
				List<IGrouping<float, Vector3>> vertexGroups = vertices.GroupBy(vertex => vertex.y).ToList();
				vertexGroups.Sort((kvp, next) => kvp.Key.CompareTo(next.Key));
				foreach (IGrouping<float, Vector3> vertexGroup in vertexGroups)
				{
					Instance.Logger.LogFatal($"Y: {vertexGroup.Key}, Count: {vertexGroup.Count()}");
				}

				_isFirst = false;
				return;
			}
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
		private static IEnumerable<CodeInstruction> ForestGiantHardPatch(IEnumerable<CodeInstruction> instructions)
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
		private static IEnumerable<CodeInstruction> SpawnBodiesHardPatch(IEnumerable<CodeInstruction> instructions)
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
		private static IEnumerable<CodeInstruction> UpdatePositionHardPatch(IEnumerable<CodeInstruction> instructions)
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
		private static IEnumerable<CodeInstruction> DamageRPCHardPatch(IEnumerable<CodeInstruction> instructions)
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
