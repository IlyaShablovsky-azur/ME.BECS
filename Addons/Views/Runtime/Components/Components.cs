namespace ME.BECS.Views {

    public struct ViewsComponentGroup {

        public static UnityEngine.Color color = UnityEngine.Color.magenta;

    }
    
    [ComponentGroup(typeof(ViewsComponentGroup))]
    public struct ViewComponent : IComponent {

        public ViewSource source;

    }

    [ComponentGroup(typeof(ViewsComponentGroup))]
    public struct AssignViewComponent : IComponent {

        public ViewSource source;
        public Ent sourceEnt;

    }

    [ComponentGroup(typeof(ViewsComponentGroup))]
    public struct IsViewRequested : IComponent {}

    [ComponentGroup(typeof(ViewsComponentGroup))]
    public struct InstantiateViewComponent : IConfigComponentStatic, IConfigInitialize {

        public View view;
        
        public void OnInitialize(in Ent ent) {

            ent.InstantiateView(this.view);

        }

    }

    [ComponentGroup(typeof(ViewsComponentGroup))]
    public struct InstantiateAvatarViewComponent : IConfigComponentStatic, IConfigInitialize {

        [System.Serializable]
        public struct AnimationData {

            [System.Serializable]
            public struct FirePoint {

                public uint id;
                public Unity.Mathematics.float3 position;
                public Unity.Mathematics.quaternion rotation;

            }

            public uint animationId;
            public FirePoint firePoint;
            public uint fireFrame;

        }

        [System.Serializable]
        public struct AnimatorData {

            public View view;
            public MemArrayAuto<AnimationData> points;

            public bool GetAnimationData(uint animationId, out AnimationData animationData) {
                for (uint i = 0; i < this.points.Length; ++i) {
                    var point = this.points[i];
                    if (point.animationId == animationId) {
                        animationData = point;
                        return true;
                    }
                }
                animationData = default;
                return false;
            }

        }

        public AnimatorData animatorData;

        public void OnInitialize(in Ent ent) {

            ent.InstantiateView(this.animatorData.view);

        }

    }

    [ComponentGroup(typeof(ViewsComponentGroup))]
    public struct MeshFilterComponent : IComponent {

        public RuntimeObjectReference<UnityEngine.Mesh> mesh;

    }

    [ComponentGroup(typeof(ViewsComponentGroup))]
    public struct MeshRendererComponent : IComponent {

        public RuntimeObjectReference<UnityEngine.Material> material;
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        public int receiveShadows;
        public int layer;
        public uint renderingLayerMask;
        public int rendererPriority;
        public int instanceID;

    }

}