﻿using NetMud.Data.EntityBackingData;
using NetMud.Data.Game;
using NetMud.DataAccess;
using NetMud.DataStructure.Base.Place;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NetMud
{
    public class GameConfig
    {
        public static void PreloadSupportingEntities()
        {
            var liveWorld = new LiveCache();

            //Rooms, paths, spawns (objs then mobs)
            liveWorld.PreLoadAll<RoomData>();
            liveWorld.PreLoadAll<PathData>();

            NetMud.Websock.Server.StartServer("localhost", 2929);
        }
    }
}