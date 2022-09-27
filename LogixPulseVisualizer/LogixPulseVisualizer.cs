using HarmonyLib;
using NeosModLoader;
using System;
using System.Reflection;
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
        private static ModConfigurationKey<bool> KEY_ENABLED = new("enabled", "When enabled, pulses will have visuals.", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<float?> KEY_HUE = new("hue", "When enabled, a new pulse will set the global hue rotation to the value of this key. 0-1", () => 1f);
        private static Dictionary<MeshRenderer, KeyValuePair<Coroutine, Action>> corutines = new();
        private static MethodInfo patchedMethod = typeof(Impulse).GetMethod("Trigger");
        private static MethodInfo patchMethod = typeof(LogixPulseVisualizerPatch).GetMethod("Postfix");
        private static Harmony harmony;
        private static ModConfiguration config;
        public override void OnEngineInit()
        {
            harmony = new Harmony("me.art0007i.LogixPulseVisualizer");
            config = GetConfiguration();
            config.OnThisConfigurationChanged += ChangeHandler;
            if (config.GetValue(KEY_ENABLED))
            {
                harmony.Patch(patchedMethod, postfix: new HarmonyMethod(patchMethod));
            }
        }

        private void ChangeHandler(ConfigurationChangedEvent configurationChangedEvent)
        {
            if (configurationChangedEvent.Key == KEY_ENABLED)
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
                    if (obj2 != null && obj2.TargetSlot.Target != null)
                    {
                        MeshRenderer renderer = ((SyncRef<Slot>)obj2.GetSyncMember("WireSlot")).Target?.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            World world = __instance.World;
                            KeyValuePair<Coroutine, Action> coroutine;
                            if (corutines.TryGetValue(renderer, out coroutine))
                            {
                                coroutine.Key.Stop();
                                coroutine.Value();
                            }
                            IAssetProvider<Material> old = renderer.Material.Target;
                            renderer.Materials[0] = GetMatertial(world);
                            Action action = () => { renderer.Materials[0] = old; corutines.Remove(renderer); };
                            corutines.Add(renderer, new(world.RunInSeconds(1f, action), action));
                        }
                    }
                }
            }
        }
        static FresnelMaterial GetMatertial(World world)
        {
            Slot slot = world.AssetsSlot.FindOrAdd("LogixAssets");

            const string key = "PulseVisualMaterial";
            FresnelMaterial fresnelMaterial = world.KeyOwner(key) as FresnelMaterial;
            if (fresnelMaterial == null)
            {
                fresnelMaterial = slot.AttachComponent<FresnelMaterial>();
                fresnelMaterial.AssignKey(key, 1, false);
                fresnelMaterial.BlendMode.Value = BlendMode.Alpha;
                fresnelMaterial.ZWrite.Value = ZWrite.On;
                fresnelMaterial.Sidedness.Value = Sidedness.Double;
                StaticTexture2D wireTexture = LogixHelper.GetWireTexture(world, 1, true);
                fresnelMaterial.NearTexture.Target = wireTexture;
                fresnelMaterial.FarTexture.Target = wireTexture;
                float2 value = new float2(0.5f, 1f);
                fresnelMaterial.NearTextureScale.Value = value;
                fresnelMaterial.FarTextureScale.Value = value;
                fresnelMaterial.FarColor.Value.MulRGB(.5f).MulA(.8f);
            }

            const string gradiantKey = key + "Gradiant";
            ValueGradientDriver<color> gradiant = world.KeyOwner(gradiantKey) as ValueGradientDriver<color>;
            if (gradiant == null)
            {
                gradiant = slot.AttachComponent<ValueGradientDriver<color>>();
                gradiant.AssignKey(gradiantKey, 1, false);
                gradiant.AddPoint(0f, color.Red);
                gradiant.AddPoint(1f / 6f, color.Yellow);
                gradiant.AddPoint(1f / 3f, color.Green);
                gradiant.AddPoint(0.5f, color.Cyan);
                gradiant.AddPoint(2f / 3f, color.Blue);
                gradiant.AddPoint(5f / 6f, color.Magenta);
                gradiant.AddPoint(1f, color.Red);
                gradiant.Target.Target = fresnelMaterial.NearColor;
            }
            else if (gradiant.Target.Target != fresnelMaterial.NearColor) gradiant.Target.Target = fresnelMaterial.NearColor;

            const string pannerKey = key + "Panner";
            Panner1D panner = world.KeyOwner(pannerKey) as Panner1D;
            if (panner == null)
            {
                panner = slot.AttachComponent<Panner1D>();
                panner.AssignKey(pannerKey, 1, false);
                panner.Target = gradiant.Progress;
                panner.Speed = 1f;
                panner.Repeat = 1f;
            }
            else if (panner.Target != gradiant.Progress)
            {
                panner.Target = gradiant.Progress;
            }

            if (config.GetValue(KEY_HUE).HasValue) panner.PreOffset = (panner.Repeat - (panner.Position - panner.PreOffset) + config.GetValue(KEY_HUE).Value) % panner.Repeat;
            world.RunInUpdates(0, () => panner.Offset = 0f); // offset likes to randomly become != 0; sometimes seting it instantly doesnt work  ¯\_(ツ)_/¯
            return fresnelMaterial;
        }
    }
}