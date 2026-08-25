using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;

namespace MysteryRooms.Editor
{
    public class RoomBuilderEditor : EditorWindow
    {
        [MenuItem("Mystery Rooms/Generate 4-Room Layout")]
        public static void GenerateRooms()
        {
            GameObject levelRoot = new GameObject("MysteryMansion_Level");
            
            Vector3 roomSize = new Vector3(12f, 5f, 12f);
            Vector3 bossRoomSize = new Vector3(16f, 7f, 16f);
            Vector3 hallwaySize = new Vector3(4f, 4f, 4f);

            GameObject entrance = CreateRoom("Room_EntranceHall", new Vector3(0, 0, 0), roomSize, levelRoot.transform);
            GameObject westChamber = CreateRoom("Room_WestChamber", new Vector3(-16, 0, 0), roomSize, levelRoot.transform);
            GameObject eastChamber = CreateRoom("Room_EastChamber", new Vector3(16, 0, 0), roomSize, levelRoot.transform);
            GameObject burialChamber = CreateRoom("Room_BurialChamber", new Vector3(0, 0, 18), bossRoomSize, levelRoot.transform);

            CreateHallway("Hallway_West", new Vector3(-8, 0, 0), new Vector3(8, 4, 3), levelRoot.transform);
            CreateHallway("Hallway_East", new Vector3(8, 0, 0), new Vector3(8, 4, 3), levelRoot.transform);
            CreateHallway("Hallway_North", new Vector3(0, 0, 8), new Vector3(3, 4, 8), levelRoot.transform);

            CreateContainer("entrance_hall", entrance.transform);
            CreateContainer("west_chamber", westChamber.transform);
            CreateContainer("east_chamber", eastChamber.transform);
            CreateContainer("burial_chamber", burialChamber.transform);

            Debug.Log("✅ Mystery Rooms Layout Generated Successfully!");
        }

        private static GameObject CreateRoom(string name, Vector3 position, Vector3 size, Transform parent)
        {
            ProBuilderMesh pbMesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            pbMesh.gameObject.name = name;
            pbMesh.transform.position = position;
            pbMesh.transform.SetParent(parent);

            foreach (var face in pbMesh.faces)
            {
                face.Reverse();
            }
            pbMesh.ToMesh();
            pbMesh.Refresh();

            pbMesh.gameObject.AddComponent<MeshCollider>();
            pbMesh.gameObject.layer = 0; 

            return pbMesh.gameObject;
        }

        private static void CreateHallway(string name, Vector3 position, Vector3 size, Transform parent)
        {
            ProBuilderMesh pbMesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            pbMesh.gameObject.name = name;
            pbMesh.transform.position = position;
            pbMesh.transform.SetParent(parent);

            foreach (var face in pbMesh.faces)
            {
                face.Reverse();
            }
            pbMesh.ToMesh();
            pbMesh.Refresh();

            pbMesh.gameObject.AddComponent<MeshCollider>();
        }

        private static void CreateContainer(string name, Transform parent)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent);
            container.transform.localPosition = Vector3.zero; 
        }
    }
}
