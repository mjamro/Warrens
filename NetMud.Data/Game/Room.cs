﻿using NetMud.DataAccess;
using NetMud.DataStructure.Base.Entity;
using NetMud.DataStructure.Base.EntityBackingData;
using NetMud.DataStructure.Base.Place;
using NetMud.DataStructure.Base.Supporting;
using NetMud.DataStructure.Behaviors.Rendering;
using NetMud.DataStructure.SupportingClasses;
using NetMud.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetMud.Data.Game
{
    public class Room : EntityPartial, IRoom
    {
        public Room()
        {
            ObjectsInRoom = new EntityContainer<IObject>();
            MobilesInRoom = new EntityContainer<IMobile>();
            Pathways = new EntityContainer<IPath>();
        }

        public Room(IRoomData room)
        {
            ObjectsInRoom = new EntityContainer<IObject>();
            MobilesInRoom = new EntityContainer<IMobile>();
            Pathways = new EntityContainer<IPath>();

            //Yes it's its own datatemplate and currentLocation
            DataTemplate = room;

            GetFromWorldOrSpawn();
        }
        
        #region Container
        public EntityContainer<IObject> ObjectsInRoom { get; set; }
        public EntityContainer<IMobile> MobilesInRoom { get; set; }
        public EntityContainer<IPath> Pathways { get; set; }

        public IEnumerable<T> GetContents<T>()
        {
            var implimentedTypes = DataUtility.GetAllImplimentingedTypes(typeof(T));

            var contents = new List<T>();

            if (implimentedTypes.Contains(typeof(IMobile)))
                contents.AddRange(GetContents<T>("mobiles"));
            
            if (implimentedTypes.Contains(typeof(IObject)))
                contents.AddRange(GetContents<T>("objects"));

            if (implimentedTypes.Contains(typeof(IPath)))
                contents.AddRange(GetContents<T>("pathways"));

            return contents;
        }

        public IEnumerable<T> GetContents<T>(string containerName)
        {
            switch (containerName)
            {
                case "mobiles":
                    return MobilesInRoom.EntitiesContained.Select(ent => (T)ent);
                case "objects":
                    return ObjectsInRoom.EntitiesContained.Select(ent => (T)ent);
                case "pathways":
                    return Pathways.EntitiesContained.Select(ent => (T)ent);
            }

            return Enumerable.Empty<T>();
        }

        public string MoveTo<T>(T thing)
        {
            return MoveTo<T>(thing, String.Empty);
        }

        public string MoveTo<T>(T thing, string containerName)
        {
            var implimentedTypes = DataUtility.GetAllImplimentingedTypes(typeof(T));

            if (implimentedTypes.Contains(typeof(IObject)))
            {
                var obj = (IObject)thing;

                if (ObjectsInRoom.Contains(obj))
                    return "That is already in the container";

                ObjectsInRoom.Add(obj);
                obj.CurrentLocation = this;
                return String.Empty;
            }

            if (implimentedTypes.Contains(typeof(IMobile)))
            {
                var obj = (IMobile)thing;

                if (MobilesInRoom.Contains(obj))
                    return "That is already in the container";

                MobilesInRoom.Add(obj);
                obj.CurrentLocation = this;
                return String.Empty;
            }

            if (implimentedTypes.Contains(typeof(IPath)))
            {
                var obj = (IPath)thing;

                if (Pathways.Contains(obj))
                    return "That is already in the container";

                Pathways.Add(obj);
                obj.CurrentLocation = this;
                return String.Empty;
            }


            return "Invalid type to move to container.";
        }

        public string MoveFrom<T>(T thing)
        {
            return MoveFrom<T>(thing, String.Empty);
        }

        public string MoveFrom<T>(T thing, string containerName)
        {
            var implimentedTypes = DataUtility.GetAllImplimentingedTypes(typeof(T));

            if (implimentedTypes.Contains(typeof(IObject)))
            {
                var obj = (IObject)thing;

                if (!ObjectsInRoom.Contains(obj))
                    return "That is not in the container";

                ObjectsInRoom.Remove(obj);
                obj.CurrentLocation = null;
                return String.Empty;
            }

            if (implimentedTypes.Contains(typeof(IMobile)))
            {
                var obj = (IMobile)thing;

                if (!MobilesInRoom.Contains(obj))
                    return "That is not in the container";

                MobilesInRoom.Remove(obj);
                obj.CurrentLocation = null;
                return String.Empty;
            }

            if (implimentedTypes.Contains(typeof(IPath)))
            {
                var obj = (IPath)thing;

                if (!Pathways.Contains(obj))
                    return "That is not in the container";

                Pathways.Remove(obj);
                obj.CurrentLocation = null;
                return String.Empty;
            }

            return "Invalid type to move from container.";
        }
        #endregion

        public override IEnumerable<string> RenderToLook()
        {
            var sb = new List<string>();

            sb.Add(string.Format("<span style=\"color: orange\">{0}</span>", DataTemplate.Name));
            sb.Add(string.Empty.PadLeft(DataTemplate.Name.Length, '-'));

            return sb;
        }

        public void GetFromWorldOrSpawn()
        {
            var liveWorld = new LiveCache();

            //Try to see if they are already there
            var me = liveWorld.Get<IRoom>(DataTemplate.ID, typeof(IRoom));

            //Isn't in the world currently
            if (me == default(IRoom))
                SpawnNewInWorld();
            else
            {
                BirthMark = me.BirthMark;
                Keywords = me.Keywords;
                Birthdate = me.Birthdate;
                CurrentLocation = me.CurrentLocation;
                DataTemplate = me.DataTemplate;
            }
        }

        public override void SpawnNewInWorld()
        {
            //TODO: will rooms ever be contained by something else?
            SpawnNewInWorld(this);
        }

        public override void SpawnNewInWorld(IContains spawnTo)
        {
            var liveWorld = new LiveCache();
            var roomTemplate = (IRoomData)DataTemplate;

            BirthMark = Birthmarker.GetBirthmark(roomTemplate);
            Keywords = new string[] { roomTemplate.Name.ToLower() };
            Birthdate = DateTime.Now;
            CurrentLocation = spawnTo;
        }
    }
}