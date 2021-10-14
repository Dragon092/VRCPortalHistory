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

namespace VRCPortalHistory
{
    public class VRCPortalHistoryMod : MelonMod
    {
        private static ApiWorld last_apiWorld = null;
        private static ApiWorldInstance last_apiWorldInstance = null;

        public override void OnApplicationStart()
        {
            PortalUtils.Init();

            HarmonyInstance.Patch(PortalUtils.enterPortal, prefix: new HarmonyMethod(typeof(VRCPortalHistoryMod).GetMethod("OnPortalEnter", BindingFlags.Static | BindingFlags.Public)));
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
            if(last_apiWorld is null || last_apiWorldInstance is null)
            {
                MelonLogger.Msg("Error: no last_apiWorld or no last_apiWorldInstance");
                return;
            }

            Transform playerTransform = Utilities.GetLocalPlayerTransform();

            Utilities.CreatePortal(last_apiWorld, last_apiWorldInstance, playerTransform.position, playerTransform.forward, true);
        }
    }
}
