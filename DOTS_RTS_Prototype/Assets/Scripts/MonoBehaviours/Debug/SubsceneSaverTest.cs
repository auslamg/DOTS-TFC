using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using System.IO;
using System;

public class SubsceneSaverTest : MonoBehaviour
{
    private string SavePath => Path.Combine(Application.persistentDataPath, "ecs_save.dat");

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            SaveWorld();
        }
    }

    void SaveWorld()
    {
        var srcWorld = World.DefaultGameObjectInjectionWorld;
        var srcEntityManager = srcWorld.EntityManager;

        // 1. Create clean save world
        var saveWorld = new World("SaveWorld");
        var dstEntityManager = saveWorld.EntityManager;

        // 2. Copy only saveable entities
        CopySaveableEntities(srcEntityManager, dstEntityManager);

        // 3. Serialize save world
        using var stream = new FileStream(SavePath, FileMode.Create, FileAccess.Write);
        using var writer = new MemoryBinaryWriter(dstEntityManager);

        SerializeUtility.SerializeWorld(dstEntityManager, writer);

        // 4. Cleanup
        saveWorld.Dispose();

        Debug.Log($"Saved ECS world to {SavePath}");
    }

    void CopySaveableEntities(EntityManager src, EntityManager dst)
    {
        var query = src.CreateEntityQuery(typeof(Saveable));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        
        var copySettings = new EntityManager
        {
            // keeps it simple: no system state, no internal stuff
        };

        foreach (var entity in entities)
        {
            var newEntity = dst.CreateEntity();

            // Copy all components except unsafe ones
            dst.AddComponent<Saveable>(newEntity);

            // Example: manually copy data components
            if (src.HasComponent<Translation>(entity))
                dst.AddComponentData(newEntity, src.GetComponentData<Translation>(entity));

            if (src.HasComponent<Rotation>(entity))
                dst.AddComponentData(newEntity, src.GetComponentData<Rotation>(entity));

            // Add your gameplay components here
            // e.g. Health, Inventory, AIState, etc.
        }

        entities.Dispose();
    }
}

partial struct Saveable : IComponentData
{
    
}

partial struct Translation : IComponentData
{

}

partial struct Rotation : IComponentData
{

}