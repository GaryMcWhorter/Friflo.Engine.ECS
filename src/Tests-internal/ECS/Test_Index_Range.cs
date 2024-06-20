﻿using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Index;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable InconsistentNaming
namespace Internal.ECS {
public static class Test_Index_Range
{
    
    [ComponentIndex(typeof(ValueInRangeIndex<>))]
    internal struct IndexedIntRange : IIndexedComponent<int> {
        public      int     GetIndexedValue() => value;
        internal    int     value;
    
        public override string ToString() => value.ToString();
    }
    
    [Test]
    public static void Test_Index_Range_Add_Remove()
    {
        var world = new EntityStore();
        var entity1 = world.CreateEntity(new Position());
        var entity2 = world.CreateEntity(new Position());
        var entity3 = world.CreateEntity(new Position());
        
        entity1.AddComponent(new IndexedIntRange { value  = 100 });
        entity2.AddComponent(new IndexedIntRange { value  = 200 });
        entity3.AddComponent(new IndexedIntRange { value  = 300 });
        
        var query0 = world.Query<IndexedIntRange, Position>().ValueInRange<IndexedIntRange, int>(0,    99);
        var query1 = world.Query<IndexedIntRange, Position>().ValueInRange<IndexedIntRange, int>(100, 100);
        var query2 = world.Query<IndexedIntRange, Position>().ValueInRange<IndexedIntRange, int>(100, 200);
        var query3 = world.Query<IndexedIntRange, Position>().ValueInRange<IndexedIntRange, int>(100, 300);
        var query4 = world.Query<IndexedIntRange, Position>().ValueInRange<IndexedIntRange, int>(900, 999);
        {
            int count = 0;
            query3.ForEachEntity((ref IndexedIntRange _, ref Position _, Entity _) => {
                // AreEqual(++count, entity.Id); // TODO set start in MoveNext() new ChunkEntities (...)
                ++count;
            });
            AreEqual(3, count);
        }
        AreEqual(0, query0.Entities.Count);     AreEqual(new int[] {         }, query0.Entities.ToIds());
        AreEqual(1, query1.Entities.Count);     AreEqual(new int[] { 1       }, query1.Entities.ToIds());
        AreEqual(2, query2.Entities.Count);     AreEqual(new int[] { 1, 2    }, query2.Entities.ToIds());
        AreEqual(3, query3.Entities.Count);     AreEqual(new int[] { 1, 2, 3 }, query3.Entities.ToIds());
        AreEqual(0, query4.Entities.Count);     AreEqual(new int[] {         }, query4.Entities.ToIds());
    }
}

}
