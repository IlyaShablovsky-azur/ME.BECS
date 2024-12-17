
namespace ME.BECS {
    
    using BURST = Unity.Burst.BurstCompileAttribute;
    using Unity.Collections.LowLevel.Unsafe;
    using ME.BECS.Jobs;
    using ME.BECS.Transforms;
    using Unity.Jobs;
    using Unity.Mathematics;
    using static Cuts;

    public struct QuadTreeElement : IComponent {

        public int treeIndex;
        public float radius;
        public bool ignoreY;

    }

    [EditorComment("Used by QuadTreeInsertSystem to filter entities by treeIndex")]
    public struct QuadTreeAspect : IAspect {
        
        public Ent ent { get; set; }

        [QueryWith]
        public AspectDataPtr<QuadTreeElement> quadTreeElementPtr;

        public readonly ref QuadTreeElement quadTreeElement => ref this.quadTreeElementPtr.value.Get(this.ent.id, this.ent.gen);
        public readonly ref readonly QuadTreeElement readQuadTreeElement => ref this.quadTreeElementPtr.value.Read(this.ent.id, this.ent.gen);
        public readonly int treeIndex => this.readQuadTreeElement.treeIndex;

    }
    
    [BURST(CompileSynchronously = true)]
    public unsafe struct QuadTreeInsertSystem : IAwake, IUpdate, IDestroy {

        public float3 mapPosition;
        public float3 mapSize;
        
        [NativeDisableUnsafePtrRestriction]
        private UnsafeList<safe_ptr> quadTrees;
        public readonly uint treesCount => (uint)this.quadTrees.Length;

        [BURST(CompileSynchronously = true)]
        public struct CollectJob : IJobForAspects<QuadTreeAspect, TransformAspect> {
            
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<safe_ptr> quadTrees;

            public void Execute(in JobInfo jobInfo, in Ent ent, ref QuadTreeAspect quadTreeAspect, ref TransformAspect tr) {
                
                var tree = (safe_ptr<NativeTrees.NativeOctree<Ent>>)this.quadTrees[quadTreeAspect.treeIndex];
                if (tr.IsCalculated == false) return;
                var pos = tr.GetWorldMatrixPosition();
                if (quadTreeAspect.readQuadTreeElement.ignoreY == true) pos.y = 0f;
                tree.ptr->Add(tr.ent, new NativeTrees.AABB(pos - quadTreeAspect.readQuadTreeElement.radius, pos + quadTreeAspect.readQuadTreeElement.radius));
                
            }

        }

        [BURST(CompileSynchronously = true)]
        public struct ApplyJob : Unity.Jobs.IJobParallelFor {

            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<safe_ptr> quadTrees;

            public void Execute(int index) {

                var tree = (safe_ptr<NativeTrees.NativeOctree<Ent>>)this.quadTrees[index];
                tree.ptr->Rebuild();
                
            }

        }
        
        [BURST(CompileSynchronously = true)]
        public struct ClearJob : Unity.Jobs.IJobParallelFor {

            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<safe_ptr> quadTrees;

            public void Execute(int index) {

                var item = (safe_ptr<NativeTrees.NativeOctree<Ent>>)this.quadTrees[index];
                item.ptr->Clear();
                
            }

        }

        public readonly safe_ptr<NativeTrees.NativeOctree<Ent>> GetTree(int treeIndex) {

            return (safe_ptr<NativeTrees.NativeOctree<Ent>>)this.quadTrees[treeIndex];

        }

        public int AddTree() {

            var size = new NativeTrees.AABB(this.mapPosition, this.mapPosition + this.mapSize);
            this.quadTrees.Add((safe_ptr)_make(new NativeTrees.NativeOctree<Ent>(size, Constants.ALLOCATOR_PERSISTENT_ST.ToAllocator)));
            return this.quadTrees.Length - 1;

        }

        public void OnAwake(ref SystemContext context) {

            this.quadTrees = new UnsafeList<safe_ptr>(10, Constants.ALLOCATOR_PERSISTENT_ST.ToAllocator);
            
        }

        public void OnUpdate(ref SystemContext context) {

            var clearJob = new ClearJob() {
                quadTrees = this.quadTrees,
            };
            var clearJobHandle = clearJob.Schedule(this.quadTrees.Length, 1, context.dependsOn);
            
            var handle = context.Query(clearJobHandle).AsParallel().Schedule<CollectJob, QuadTreeAspect, TransformAspect>(new CollectJob() {
                quadTrees = this.quadTrees,
            });

            var job = new ApplyJob() {
                quadTrees = this.quadTrees,
            };
            var resultHandle = job.Schedule(this.quadTrees.Length, 1, handle);
            //var resultHandle = handle;
            context.SetDependency(resultHandle);

        }

        public void OnDestroy(ref SystemContext context) {

            for (int i = 0; i < this.quadTrees.Length; ++i) {
                var item = (safe_ptr<NativeTrees.NativeOctree<Ent>>)this.quadTrees[i];
                item.ptr->Dispose();
                _free(item);
            }

            this.quadTrees.Dispose();

        }

        public readonly void FillNearest<T>(ref QuadTreeQueryAspect query, in TransformAspect tr, in T subFilter = default) where T : struct, ISubFilter<Ent> {
            
            if (tr.IsCalculated == false) return;
            var q = query.readQuery;
            var worldPos = tr.GetWorldMatrixPosition();
            var worldRot = tr.GetWorldMatrixRotation();
            var sector = new MathSector(worldPos, worldRot, query.readQuery.sector);
            var ent = tr.ent;
                
            // clean up results
            if (query.results.results.IsCreated == true) query.results.results.Clear();
            if (query.results.results.IsCreated == false) query.results.results = new ListAuto<Ent>(query.ent, q.nearestCount > 0u ? q.nearestCount : 1u);

            if (q.nearestCount == 1u) {
                var nearest = this.GetNearestFirst(q.treeMask, in ent, in worldPos, in sector, q.minRangeSqr, q.rangeSqr, q.ignoreSelf == 1 ? true : false, q.ignoreY == 1 ? true : false, in subFilter);
                if (nearest.IsAlive() == true) query.results.results.Add(nearest);
            } else {
                this.GetNearest(q.treeMask, q.nearestCount, ref query.results.results, in ent, in worldPos, in sector, q.minRangeSqr, q.rangeSqr, q.ignoreSelf == 1 ? true : false, q.ignoreY == 1 ? true : false, in subFilter);
            }
            
        }
        
        public readonly Ent GetNearestFirst(int mask, in Ent selfEnt = default, in float3 worldPos = default, in MathSector sector = default, float minRangeSqr = default,
                                      float rangeSqr = default, bool ignoreSelf = default, bool ignoreY = default) {
            return this.GetNearestFirst(mask, in selfEnt, in worldPos, in sector, minRangeSqr, rangeSqr, ignoreSelf, ignoreY, new AlwaysTrueSubFilter());
        }

        public readonly Ent GetNearestFirst<T>(int mask, in Ent selfEnt = default, in float3 worldPos = default, in MathSector sector = default, float minRangeSqr = default, float rangeSqr = default, bool ignoreSelf = default, bool ignoreY = default, in T subFilter = default) where T : struct, ISubFilter<Ent> {

            const uint nearestCount = 1u;
            var heap = new ME.BECS.NativeCollections.NativeMinHeapEnt(this.treesCount, Constants.ALLOCATOR_TEMP);
            // for each tree
            for (int i = 0; i < this.treesCount; ++i) {
                if ((mask & (1 << i)) == 0) {
                    continue;
                }
                ref var tree = ref *this.GetTree(i).ptr;
                {
                    var visitor = new OctreeNearestAABBVisitor<Ent, T>() {
                        subFilter = subFilter,
                        sector = sector,
                        ignoreSelf = ignoreSelf,
                        ignore = selfEnt,
                    };
                    tree.Nearest(worldPos, minRangeSqr, rangeSqr, ref visitor, new AABBDistanceSquaredProvider<Ent>() { ignoreY = ignoreY });
                    if (visitor.found == true) {
                        heap.Push(new ME.BECS.NativeCollections.MinHeapNodeEnt(visitor.nearest, math.lengthsq(worldPos - visitor.nearest.GetAspect<TransformAspect>().GetWorldMatrixPosition())));
                    }
                }
            }
            
            {
                var max = math.min(nearestCount, heap.Count);
                if (max > 0u) return heap[heap.Pop()].data;
            }

            return default;

        }

        public readonly void GetNearest(int mask, ushort nearestCount, ref ListAuto<Ent> results, in Ent selfEnt, in float3 worldPos, in MathSector sector, float minRangeSqr, float rangeSqr, bool ignoreSelf, bool ignoreY) {
            this.GetNearest(mask, nearestCount, ref results, in selfEnt, in worldPos, in sector, minRangeSqr, rangeSqr, ignoreSelf, ignoreY, new AlwaysTrueSubFilter());
        }

        public readonly void GetNearest<T>(int mask, ushort nearestCount, ref ListAuto<Ent> results, in Ent selfEnt, in float3 worldPos, in MathSector sector, float minRangeSqr, float rangeSqr, bool ignoreSelf, bool ignoreY, in T subFilter = default) where T : struct, ISubFilter<Ent> {

            if (nearestCount > 0u) {

                var heap = new ME.BECS.NativeCollections.NativeMinHeapEnt(nearestCount * this.treesCount, Constants.ALLOCATOR_TEMP);
                // for each tree
                for (int i = 0; i < this.treesCount; ++i) {
                    if ((mask & (1 << i)) == 0) {
                        continue;
                    }
                    ref var tree = ref *this.GetTree(i).ptr;
                    {
                        var visitor = new OctreeKNearestAABBVisitor<Ent, T>() {
                            subFilter = subFilter,
                            sector = sector,
                            results = new UnsafeHashSet<Ent>(nearestCount, Constants.ALLOCATOR_TEMP),
                            max = nearestCount,
                            ignoreSelf = ignoreSelf,
                            ignore = selfEnt,
                        };
                        tree.Nearest(worldPos, minRangeSqr, rangeSqr, ref visitor, new AABBDistanceSquaredProvider<Ent>() { ignoreY = ignoreY });
                        foreach (var item in visitor.results) {
                            heap.Push(new ME.BECS.NativeCollections.MinHeapNodeEnt(item, math.lengthsq(worldPos - item.GetAspect<TransformAspect>().GetWorldMatrixPosition())));
                        }
                    }
                }

                var max = math.min(nearestCount, heap.Count);
                for (uint i = 0u; i < max; ++i) {
                    results.Add(heap[heap.Pop()].data);
                }

            } else {
                
                // select all units
                var heap = new ME.BECS.NativeCollections.NativeMinHeapEnt(this.treesCount, Constants.ALLOCATOR_TEMP);
                // for each tree
                for (int i = 0; i < this.treesCount; ++i) {
                    if ((mask & (1 << i)) == 0) {
                        continue;
                    }
                    ref var tree = ref *this.GetTree(i).ptr;
                    {
                        var visitor = new RangeAABBUniqueVisitor<Ent, T>() {
                            subFilter = subFilter,
                            sector = sector,
                            results = new UnsafeHashSet<Ent>(nearestCount, Constants.ALLOCATOR_TEMP),
                            rangeSqr = rangeSqr,
                            max = nearestCount,
                            ignoreSelf = ignoreSelf,
                            ignore = selfEnt,
                        };
                        var range = math.sqrt(rangeSqr);
                        tree.Range(new NativeTrees.AABB(worldPos - range, worldPos + range), ref visitor);
                        heap.EnsureCapacity((uint)visitor.results.Count);
                        foreach (var item in visitor.results) {
                            if (item.IsAlive() == false) continue;
                            heap.Push(new ME.BECS.NativeCollections.MinHeapNodeEnt(item, math.lengthsq(worldPos - item.GetAspect<TransformAspect>().GetWorldMatrixPosition())));
                        }
                    }
                }

                for (uint i = 0u; i < heap.Count; ++i) {
                    results.Add(heap[heap.Pop()].data);
                }
                
            }

        }

    }

}