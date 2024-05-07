﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;
// Hard Rule! file must not have any dependency a to a specific game engine. E.g. Unity, Godot, Monogame, ...

// ReSharper disable UseCollectionExpression
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToAutoProperty
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Systems
{
    public class SystemRoot : SystemGroup
    {
    #region properties
        public  ReadOnlyList<EntityStore>   Stores      => stores;
        public override string              ToString()  => $"'{Name}' Root - systems: {systemMap.Count}";
        #endregion

    #region internal fields
        [Browse(Never)] private readonly    Dictionary<int, BaseSystem> systemMap = new();
        [Browse(Never)] private             int                         nextId;
        [Browse(Never)] internal            ReadOnlyList<EntityStore>   stores;
        [Browse(Never)] internal            ReadOnlyList<BaseSystem>    systemBuffer;
        #endregion

        
    #region constructors
        public SystemRoot (string name) : base (name) {
            SetRoot(this);
            systemBuffer    = new ReadOnlyList<BaseSystem> (Array.Empty<BaseSystem>());
            stores          = new ReadOnlyList<EntityStore>(Array.Empty<EntityStore>());
        }
        
        public SystemRoot (EntityStore store, string name = null) : base (name ?? "Systems") {
            SetRoot(this);
            systemBuffer    = new ReadOnlyList<BaseSystem> (Array.Empty<BaseSystem>());
            stores          = new ReadOnlyList<EntityStore>(Array.Empty<EntityStore>());
            AddStore(store);
        }
        #endregion

    #region store: add / remove
        public void AddStore(EntityStore entityStore)
        {
            if (entityStore == null) throw new ArgumentNullException(nameof(entityStore));
            stores.Add(entityStore);
            var rootSystems = GetSubSystems(ref systemBuffer);
            foreach (var system in rootSystems) {
                system.AddStoreInternal(entityStore);        
            }
        }
        
        public void RemoveStore(EntityStore entityStore)
        {
            if (entityStore == null) throw new ArgumentNullException(nameof(entityStore));
            stores.Remove(entityStore);
            var rootSystems = GetSubSystems(ref systemBuffer);
            foreach (var system in rootSystems) {
                system.RemoveStoreInternal(entityStore);        
            }
        }
        #endregion
        
    #region system: add / remove    
        internal void AddSystemToRoot(BaseSystem system)
        {
            var map = systemMap;
            if (system.id != 0) {
                if (map.TryAdd(system.Id, system)) {
                    return;
                }
            }
            while (map.ContainsKey(++nextId)) { }
            system.id = nextId;
            map.Add(nextId, system);
        }
        
        internal void RemoveSystemFromRoot(BaseSystem system)
        {
            systemMap.Remove(system.Id);
        }
        #endregion
    }
}