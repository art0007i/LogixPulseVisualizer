using HarmonyLib;
using NeosModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.LogiX;
using BaseX;

namespace LogixPulseVisualizer
{
    public class LogixPulseVisualizer : NeosMod
    {
        public override string Name => "LogixPulseVisualizer";
        public override string Author => "art0007i";
        public override string Version => "1.0.1";
        public override string Link => "https://github.com/art0007i/LogixPulseVisualizer/";

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> KEY_ENABLED = new("enabled", "When enabled, pulses will have visuals.", ()=>true);
        private static MethodInfo patchedMethod = typeof(Impulse).GetMethod("Trigger");
        private static MethodInfo patchMethod = typeof(LogixPulseVisualizerPatch).GetMethod("Postfix");
        private static Harmony harmony;
        public override void OnEngineInit()
        {
            harmony = new Harmony("me.art0007i.LogixPulseVisualizer");

            GetConfiguration().OnThisConfigurationChanged += ChangeHandler;
            if (GetConfiguration().GetValue(KEY_ENABLED))
            {
                harmony.Patch(patchedMethod, postfix: new HarmonyMethod(patchMethod));
            }
        }

        private void ChangeHandler(ConfigurationChangedEvent configurationChangedEvent)
        {
            if(configurationChangedEvent.Key == KEY_ENABLED)
            {
                if (configurationChangedEvent.Config.GetValue(KEY_ENABLED))
                {
                    harmony.Patch(patchedMethod, postfix: new HarmonyMethod(patchMethod));
                }
                else
                {
                    harmony.Unpatch(patchedMethod, patchMethod);
                }
            }
        }

        class LogixPulseVisualizerPatch
        {
            public static void Postfix(Impulse __instance)
            {
                Slot target2 = __instance.OwnerNode.ActiveVisual;
                ImpulseSourceProxy impulseSourceProxy = ((target2 != null) ? target2.GetComponentInChildren<ImpulseSourceProxy>((ImpulseSourceProxy e) => e.ImpulseSource.Target == __instance, false, false) : null);
                if (impulseSourceProxy != null)
                {
                    Slot slot = impulseSourceProxy.Slot[0];
                    ConnectionWire obj2 = ((slot != null) ? slot.GetComponent<ConnectionWire>(null, false) : null);
                    if (obj2 != null)
                    {
                        FresnelMaterial fresnelMaterial = ((SyncRef<FresnelMaterial>) obj2.GetSyncMember("Material")).Target;
                        if (fresnelMaterial != null)
                        {
                            color from = ColorHSV.Hue((float)__instance.Time.WorldTime * 0.5f);
                            color to = ((Sync<color>)obj2.GetSyncMember("TypeColor")).Value;
                            fresnelMaterial.FarColor.TweenFromTo(from, to, 1f, CurvePreset.Sine, null, null);
                            fresnelMaterial.NearColor.TweenFromTo(from, to, 1f, CurvePreset.Sine, null, null);
                        }
                    }
                }
            }
        }
    }
}