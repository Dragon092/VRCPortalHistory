using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.Core;
using UIExpansionKit.API;
using VRChatUtilityKit.Utilities;
using UnhollowerRuntimeLib.XrefScans;

namespace VRCPortalHistory
{
    public class VRCPortalHistoryMod : MelonMod
    {
        private static ApiWorld last_apiWorld = null;
        private static ApiWorldInstance last_apiWorldInstance = null;

        public override void OnApplicationStart()
        {
            HarmonyInstance.Patch(typeof(PortalInternal).GetMethods().Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && XrefUtils.CheckUsedBy(mb, "OnTriggerEnter")).First(), prefix: new HarmonyMethod(typeof(VRCPortalHistoryMod).GetMethod("OnPortalEnter", BindingFlags.Static | BindingFlags.Public)));
            HarmonyInstance.Patch(typeof(PortalInternal).GetMethod("ConfigurePortal"), postfix: new HarmonyMethod(typeof(VRCPortalHistoryMod).GetMethod("OnPortalDropped", BindingFlags.Static | BindingFlags.Public)));
            HarmonyInstance.Patch(typeof(PortalInternal).GetMethod("OnDestroy"), prefix: new HarmonyMethod(typeof(VRCPortalHistoryMod).GetMethod("OnPortalDestroyed", BindingFlags.Static | BindingFlags.Public)));

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Respawn last portal", () => respawnLastPortal());

            MelonLogger.Msg("Initialized!");
        }

        public static void OnPortalDropped(MonoBehaviour __instance, Player __3)
        {
            //cachedDroppers.Add(__instance.GetInstanceID(), __3);
            MelonLogger.Msg("OnPortalDropped");

            /*
            MelonLogger.Msg(__instance.name);

            GameObject test_go = __instance.gameObject;

            MelonLogger.Msg(test_go.name);

            PortalInternal portalInternal = test_go.GetComponentInChildren<PortalInternal>();

            if (!portalInternal)
            {
                MelonLogger.Msg("Error: no portalInternal");
                return;
            }

            string roomId = portalInternal.field_Private_String_1;
            MelonLogger.Msg(roomId);

            MelonLogger.Msg(portalInternal.field_Private_ApiWorld_0);
            var world = new ApiWorld { id = portalInternal.field_Private_ApiWorld_0.id };
            MelonLogger.Msg(world.id);
            string worldId = portalInternal.field_Private_ApiWorld_0.id;
            MelonLogger.Msg(worldId);
            int roomPop = portalInternal.field_Private_Int32_0;
            MelonLogger.Msg(roomPop);

            */

            //Transform playerTransform = Utilities.GetLocalPlayerTransform();
            //bool created = Utilities.CreatePortal(apiWorld, apiWorldInstance, playerTransform.position, playerTransform.forward, true);

            //MelonLogger.Msg(_roomId);
            //MelonLogger.Msg(__instance._idWithTags);
            //MelonLogger.Msg(__instance._playerCount);
            //MelonLogger.Msg(__instance.instigator);

            /*
            if (!(__instance is PortalInternal))
            {
                MelonLogger.Msg("Not PortalInternal");
                return;
            }

            MelonLogger.Msg("PortalInternal");

            PortalInternal portalInternal = (PortalInternal)__instance;

            
            */

        }

        public static void OnPortalDestroyed(MonoBehaviour __instance)
        {
            //cachedDroppers.Remove(__instance.GetInstanceID());
            MelonLogger.Msg("OnPortalDestroyed");

            MelonLogger.Msg(__instance.name);

            GameObject test_go = __instance.gameObject;

            MelonLogger.Msg(test_go.name);

            PortalInternal portalInternal = test_go.GetComponentInChildren<PortalInternal>();

            if (!portalInternal)
            {
                MelonLogger.Msg("Error: no portalInternal");
                return;
            }

            string roomId = portalInternal.field_Private_String_1;
            MelonLogger.Msg(roomId);

            var world = new ApiWorld { id = portalInternal.field_Private_ApiWorld_0.id };
            MelonLogger.Msg(world.id);

            ApiWorldInstance apiWorldInstance = new ApiWorldInstance(world, roomId);

            last_apiWorld = world;
            last_apiWorldInstance = apiWorldInstance;

            /*
            string worldId = portalInternal.field_Private_ApiWorld_0.id;
            MelonLogger.Msg(worldId);
            int roomPop = portalInternal.field_Private_Int32_0;
            MelonLogger.Msg(roomPop);
            */
        }

        public static bool OnPortalEnter(PortalInternal __instance)
        {
            MelonLogger.Msg("OnPortalEnter");
            return true;
        }

        private void respawnLastPortal()
        {
            if (last_apiWorld is null || last_apiWorldInstance is null)
            {
                MelonLogger.Msg("Error: no last_apiWorld or no last_apiWorldInstance");
                return;
            }

            Transform playerTransform = VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;

            Utilities.CreatePortal(last_apiWorld, last_apiWorldInstance, playerTransform.position, playerTransform.forward, true);
        }
    }

    public static class Utilities
    {
        private static CreatePortalDelegate ourCreatePortalDelegate;

        private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool withUIErrors);

        private static CreatePortalDelegate GetCreatePortalDelegate
        {
            get
            {
                if (ourCreatePortalDelegate != null) return ourCreatePortalDelegate;
                MethodInfo portalMethod = typeof(PortalInternal).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).First(
                    m => m.ReturnType == typeof(bool)
                         && m.HasParameters(typeof(ApiWorld), typeof(ApiWorldInstance), typeof(Vector3), typeof(Vector3), typeof(bool))
                         && m.XRefScanFor("admin_dont_allow_portal"));
                ourCreatePortalDelegate = (CreatePortalDelegate)Delegate.CreateDelegate(typeof(CreatePortalDelegate), portalMethod);
                return ourCreatePortalDelegate;
            }
        }

        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts)
        {
            return GetCreatePortalDelegate(apiWorld, apiWorldInstance, position, forward, showAlerts);
        }

        private static bool HasParameters(this MethodBase methodBase, params Type[] types)
        {
            ParameterInfo[] parameters = methodBase.GetParameters();
            int typesLength = types.Length;
            if (parameters.Length < typesLength) return false;

            for (var i = 0; i < typesLength; ++i)
                if (parameters[i].ParameterType != types[i])
                    return false;

            return true;
        }

        public static bool XRefScanFor(this MethodBase methodBase, string searchTerm)
        {
            return XrefScanner.XrefScan(methodBase).Any(
                xref => xref.Type == XrefType.Global && xref.ReadAsObject()?.ToString().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
