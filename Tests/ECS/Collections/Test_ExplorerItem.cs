﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Friflo.Fliox.Engine.ECS;
using Friflo.Fliox.Engine.ECS.Collections;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Collections;

public static class Test_ExplorerItem
{
    [Test]
    public static void Test_ExplorerItem_Basics()
    {
        var store       = new EntityStore(PidType.UsePidAsId);
        var root        = store.CreateEntity(1);
        var tree        = new ExplorerItemTree(root, "test");
        AreEqual("test", tree.ToString());
        
        var rootEvents  = ExplorerEvents.AddHandlerSeq(tree.RootItem, (args, seq) => {
            switch (seq) {
                case 0:     AreEqual("Add ChildIds[0] = 2",     args.AsString());   return;
                case 1:     AreEqual("Add ChildIds[1] = 4",     args.AsString());   return;
                default:    Fail("unexpected");                                     return;
            }
        });
        var child2          = store.CreateEntity(2);
        var child2Item      = tree.GetItemById(child2.Id);
        var child2Events    = ExplorerEvents.AddHandlerSeq(child2Item, (args, seq) => {
            switch (seq) {
                case 0:     AreEqual("Add ChildIds[0] = 3",     args.AsString());   return;
                case 1:     AreEqual("Remove ChildIds[0] = 3",  args.AsString());   return;
                default:    Fail("unexpected");                                     return;
            }
        });
        root.AddChild(child2);
        
        var subChild3       = store.CreateEntity(3);
        child2.AddChild     (subChild3);
        child2.RemoveChild  (subChild3);

        // --- floating entities
        var floating4       = store.CreateEntity(4);
        var floating5       = store.CreateEntity(5);
        AreEqual(TreeMembership.floating, floating4.TreeMembership);
        floating4.AddChild(floating5);      // adding entities to floating entities send no events
        
        // --- floating entity becomes treeNode
        root.AddChild(floating4);
        AreEqual(4, root.ChildIds[1]);
        // AreEqual(TreeMembership.treeNode, floating4.TreeMembership); // todo
        var floating4Item = tree.GetItemById(floating4.Id);
        floating4Item.CollectionChanged += (_, args) => {
            Fail($"unexpected event: {args}");
        };
        floating4.RemoveChild(floating5);   // fires no event as floating5 has no corresponding ExplorerItem
        
        AreEqual(2, rootEvents.seq);
        AreEqual(2, child2Events.seq);
    }
    
    [Test]
    public static void Test_ExplorerItem_CollectionChanged()
    {
        var store       = new EntityStore(PidType.UsePidAsId);
        var root        = store.CreateEntity(1);
        var tree        = new ExplorerItemTree(root, "test");
        var rootItem    = tree.RootItem;
        
        var seq = 0;
        NotifyCollectionChangedEventHandler handler = (_, args) => {
            seq++;
            AreEqual("Add ChildIds[0] = 2", args.AsString());
        };
        rootItem.CollectionChanged += handler;
        
        var child2 = store.CreateEntity(2);
        root.AddChild(child2);  // fires event
        
        rootItem.CollectionChanged -= handler;
        var child3 = store.CreateEntity(3);
        root.AddChild(child3);  // no event fired - event handler was removed;
        
        AreEqual(1, seq);
    }
    
    [Test]
    public static void Test_ExplorerItemEnumerator()
    {
        var store       = new EntityStore(PidType.UsePidAsId);
        var root        = store.CreateEntity(1);
        var tree        = new ExplorerItemTree(root, null);
        AreEqual("ExplorerTree", tree.ToString());
        var rootItem    = tree.RootItem;
        
        root.AddChild(store.CreateEntity(2));
        root.AddChild(store.CreateEntity(3));
        root.AddChild(store.CreateEntity(4));

        int n = 2;
        foreach (var child in rootItem) {
            AreEqual(n++, child.Id);
        }
        AreEqual(5, n);
        
        n = 2;
        IEnumerator<ExplorerItem> enumerator = rootItem.GetEnumerator();
        IEnumerator enumerator2 = enumerator;
        enumerator.Reset();
        while (enumerator.MoveNext()) {
            AreEqual(n, enumerator.Current!.Id);
            var current2 = enumerator2.Current as ExplorerItem; // test coverage
            AreEqual(n, current2!.Id);
            n++;
        }
        AreEqual(5, n);
        enumerator.Dispose();
        
        n = 2;
        IEnumerable enumerable = rootItem;
        foreach (var obj in enumerable) {
            var item = obj as ExplorerItem;
            AreEqual(n++, item!.Id);
        }
        AreEqual(5, n);
    }
    
    [Test]
    public static void Test_ExplorerItem_TreeDataGrid_Access()
    {
        var store       = new EntityStore(PidType.UsePidAsId);
        var root        = store.CreateEntity(1);
        var tree        = new ExplorerItemTree(root, null);
        
        store.CreateEntity(2);
        store.CreateEntity(3);
        store.CreateEntity(4);
        store.CreateEntity(5);
        store.CreateEntity(6);
        
        var rootItem = tree.RootItem;
        AreEqual("id: 1  []",   rootItem.ToString());
        AreSame(root,           rootItem.Entity);
        IsTrue  (rootItem.IsRoot);
        IsFalse (rootItem.AllowDrag);
        AreEqual("---",         rootItem.Name);
        rootItem.Name = "test";
        AreEqual("test",        rootItem.Name);
        rootItem.Name = null;
        AreEqual("---",         rootItem.Name);
        
        var rootEvents  = ExplorerEvents.AddHandlerSeq(rootItem, (args, seq) => {
            switch (seq) {
                case 0: AreEqual("Add ChildIds[0] = 2",     args.AsString());   return;
                case 1: AreEqual("Add ChildIds[1] = 3",     args.AsString());   return;
                case 2: AreEqual("Add ChildIds[2] = 4",     args.AsString());   return;
                case 3: AreEqual("Add ChildIds[3] = 5",     args.AsString());   return;
                case 4: AreEqual("Add ChildIds[4] = 6",     args.AsString());   return;
                //
                case 5: AreEqual("Remove ChildIds[0] = 2",  args.AsString());   return;
                case 6: AreEqual("Remove ChildIds[0] = 3",  args.AsString());   return;
                case 7: AreEqual("Remove ChildIds[0] = 4",  args.AsString());   return;
                case 8: AreEqual("Remove ChildIds[0] = 5",  args.AsString());   return;
                case 9: AreEqual("Remove ChildIds[0] = 6",  args.AsString());   return;
                default: Fail("unexpected");                                    return;
            }
        });
        
        ICollection<ExplorerItem>           rootICollectionGen  = rootItem;
        ICollection                         rootICollection     = rootItem;
        IList<ExplorerItem>                 rootIListGen        = rootItem;
        IList                               rootIList           = rootItem;
        IReadOnlyList<ExplorerItem>         rootReadOnlyList    = rootItem;
        IReadOnlyCollection<ExplorerItem>   rootReadOnlyCol     = rootItem;
        var item2       = tree.GetItemById(2);
        var item3       = tree.GetItemById(3);
        var item4       = tree.GetItemById(4);
        var item5       = tree.GetItemById(5);
        var item6       = tree.GetItemById(6);

        IsTrue(tree.TryGetItem(6, out var tryItem));
        AreSame(item6, tryItem);
        
        IsFalse(tree.TryGetItem(777, out tryItem));
        IsNull(tryItem);

        // --- Add() / Insert() mutations
        rootICollectionGen. Add      (item2);
        rootIList.          Add      (item3);
        rootIList.          Insert(2, item4);
        rootIListGen.       Add      (item5);
        rootIListGen.       Insert(4, item6);
        
        
        // --- ICollection<ExplorerItem> queries
        IsTrue  (rootICollectionGen.Contains(item2));
        var items = new ExplorerItem[5];
        rootICollectionGen.CopyTo(items, 0);
        var expect = new [] { item2, item3, item4, item5, item6 };
        AreEqual(expect, items);
        AreEqual(5, rootICollectionGen.Count);
        IsFalse (rootICollectionGen.IsReadOnly);
        
        // --- IList<> queries
        AreEqual(1, rootIListGen.IndexOf(item3));
        AreSame (item3, rootIListGen[1]);
        
        // --- IReadOnlyList<> queries
        AreSame (item3, rootReadOnlyList[1]);
        
        // --- IReadOnlyCollection<> queries
        AreEqual(5, rootReadOnlyCol.Count);
        
        // --- IList queries
        AreSame (item2, rootIList[0]);
        IsTrue  (rootIList.Contains(item2));
        AreEqual(1, rootIList.IndexOf(item3));
        IsFalse (rootIList.IsFixedSize);
        IsFalse (rootIList.IsReadOnly);
        
        // ---ICollection queries
        AreEqual(5, rootICollection.Count);
        IsFalse (rootICollection.IsSynchronized);
        IsNull  (rootICollection.SyncRoot);
        var items2 = new ExplorerItem[5];
        rootICollection.CopyTo(items2, 0);
        AreEqual(expect, items2);
        
        // --- Remove() / RemoveAt() mutations
        rootICollectionGen.Remove(item2);
        rootIList.Remove(item3);
        rootIList.RemoveAt(0);
        rootIListGen.Remove(item5);
        rootIListGen.RemoveAt(0);
        
        AreEqual(10, rootEvents.seq);
    }
    
    [Test]
    public static void Test_ExplorerItem_PropertyChanged()
    {
        var store       = new EntityStore(PidType.UsePidAsId);
        var root        = store.CreateEntity(1);
        var tree        = new ExplorerItemTree(root, "test-tree");
        var rootItem    = tree.GetItemById(1);
        AreSame("test-tree", rootItem.DebugTreeName);
        
        var count       = 0;
        var senderObj   = new object();
        // --- add handler
        var handler     = new PropertyChangedEventHandler((sender, args) => {
            count++;
            AreEqual("test-name", args.PropertyName);
            AreSame(senderObj, sender);
        });
        rootItem.PropertyChanged += handler;
        rootItem.PropertyChanged += (_, _) => { };
        
        var ev = new PropertyChangedEventArgs("test-name");
        rootItem.propertyChangedHandler.Invoke(senderObj, ev);
        AreEqual(1, count);
        
        // --- remove handler
        rootItem.PropertyChanged -= handler;
        rootItem.propertyChangedHandler.Invoke(senderObj, ev);
        AreEqual(1, count); // no event send to handler
    }
    
    private static string AsString(this NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action) {
            case NotifyCollectionChangedAction.Add:
                var newItem     = args.NewItems![0] as ExplorerItem;
                return $"Add ChildIds[{args.NewStartingIndex}] = {newItem!.Id}";
            case NotifyCollectionChangedAction.Remove:
                var removeItem = args.OldItems![0] as ExplorerItem;
                return $"Remove ChildIds[{args.OldStartingIndex}] = {removeItem!.Id}";
            default:
                throw new InvalidOperationException("unexpected");
        }
    }
}

