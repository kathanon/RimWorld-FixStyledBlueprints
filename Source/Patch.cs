using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace FixStyledBlueprints;

[HarmonyPatch]
public static class Patch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.Graphic), MethodType.Getter)]
    public static IEnumerable<CodeInstruction> Graphic_Transpiler(IEnumerable<CodeInstruction> original) {
        var method = AccessTools.Method(typeof(Graphic), nameof(Graphic.GetColoredVersion));
        foreach (var instr in original) {
            if (instr.Calls(method)) {
                yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instr);
                yield return CodeInstruction.Call(typeof(Patch), nameof(GetColoredVersion));
            } else {
                yield return instr;
            }
        }
    }

    public static Graphic GetColoredVersion(
            Graphic self, Shader newShader, Color newColor, Color newColorTwo, Blueprint_Build blueprint) {
        if (blueprint.def.modExtensions?.Exists(Marker.Equals) ?? false) {
            var data = blueprint.def.graphicData;
            if (data != null && blueprint.StyleDef?.Graphic != null) {
                return data.Graphic;
            }
        }
        return self.GetColoredVersion(newShader, newColor, newColorTwo);
    }


    private static BlueprintMarker Marker = new BlueprintMarker();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing")]
    public static void NewBlueprintDef_Thing(ThingDef def, bool isInstallBlueprint, ThingDef normalBlueprint, ThingDef __result) {
        if (isInstallBlueprint && normalBlueprint != null) return;
        var data = def?.building?.blueprintGraphicData;
        if (data != null && data.shaderType != ShaderTypeDefOf.EdgeDetect && __result != null) {
            (__result.modExtensions ??= new()).Add(Marker);
        }
    }
}

public class BlueprintMarker : DefModExtension {
    public bool Equals(DefModExtension other) 
        => ReferenceEquals(this, other);
}
