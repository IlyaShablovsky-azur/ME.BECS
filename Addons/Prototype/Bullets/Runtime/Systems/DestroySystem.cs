
namespace ME.BECS.Bullets {
    
    using BURST = Unity.Burst.BurstCompileAttribute;
    using ME.BECS.Jobs;
    using ME.BECS.Transforms;
    using ME.BECS.Effects;
    using ME.BECS.Units;

    [BURST(CompileSynchronously = true)]
    [UnityEngine.Tooltip("Bullets hit target")]
    [RequiredDependencies(typeof(QuadTreeQuerySystem))]
    public struct DestroySystem : IUpdate {

        [BURST(CompileSynchronously = true)]
        public struct DestroyJob : IJobAspect<BulletAspect, QuadTreeQueryAspect, TransformAspect> {

            public void Execute(in JobInfo jobInfo, ref BulletAspect bullet, ref QuadTreeQueryAspect query, ref TransformAspect tr) {

                if (bullet.config.hitRangeSqr > 0f) {

                    // use splash
                    for (uint i = 0u; i < query.results.results.Count; ++i) {
                        var unit = query.results.results[i];
                        if (unit.IsAlive() == false) continue;
                        var targetUnit = unit.GetAspect<UnitAspect>();
                        targetUnit.Hit(bullet.config.damage, bullet.component.sourceUnit, in jobInfo);
                    }

                } else if (bullet.component.targetEnt.IsAlive() == true) {
                    
                    // hit only target unit if its alive and set
                    var targetUnit = bullet.component.targetEnt.GetAspect<UnitAspect>();
                    targetUnit.Hit(bullet.config.damage, bullet.component.sourceUnit, in jobInfo);
                    
                } else if (bullet.component.targetEnt == Ent.Null) {

                    // hit first target in range because targetEnt was not set
                    if (query.results.results.Count > 0u) {
                        var unit = query.results.results[0];
                        if (unit.IsAlive() == true) {
                            var targetUnit = unit.GetAspect<UnitAspect>();
                            targetUnit.Hit(bullet.config.damage, bullet.component.sourceUnit, in jobInfo);
                        }
                    }

                }

                EffectUtils.CreateEffect(tr.position, tr.rotation, bullet.ent.ReadStatic<BulletEffectOnDestroy>().effect, in jobInfo);
                bullet.ent.DestroyHierarchy();

            }

        }
        
        public void OnUpdate(ref SystemContext context) {

            var dependsOn = context.Query().With<TargetReachedComponent>().Schedule<DestroyJob, BulletAspect, QuadTreeQueryAspect, TransformAspect>();
            context.SetDependency(dependsOn);

        }

    }

}