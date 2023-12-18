namespace ME.BECS {
    
    using INLINE = System.Runtime.CompilerServices.MethodImplAttribute;
    using Unity.Mathematics;

    internal unsafe struct RandomState : System.IDisposable {

        public State* state;
        public Random random;
        
        public RandomState(State* state) {
            this.state = state;
            this.random = new Random(this.state->random);
        }

        public void Dispose() {
            this.state->random = this.random.state;
        }

    }

    public unsafe partial struct World {

        [INLINE(256)]
        public float3 GetRandomVector3InSphere(float radius) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat3() * radius;
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float2 GetRandomVector2InCircle(float radius) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat2() * radius;
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float2 GetRandomVector2OnCircle(float radius) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat2Direction() * radius;
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float3 GetRandomVector3OnSphere(float radius) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat3Direction() * radius;
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float GetRandomValue() {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat();
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float GetRandomValue(float min, float max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat(min, max);
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float GetRandomValue(float max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat(max);
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float2 GetRandomVector2() {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat2();
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float2 GetRandomVector2(float2 min, float2 max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat2(min, max);
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float2 GetRandomVector2(float2 max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat2(max);
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float3 GetRandomVector3() {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat3();
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float3 GetRandomVector3(float3 min, float3 max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat3(min, max);
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float3 GetRandomVector3(float3 max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat3(max);
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float4 GetRandomVector4() {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat4();
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float4 GetRandomVector4(float4 min, float4 max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat4(min, max);
            rnd.Dispose();
            return result;
        }

        [INLINE(256)]
        public float4 GetRandomVector4(float4 max) {
            E.IS_IN_TICK(this.state);
            var rnd = new RandomState(this.state);
            var result = rnd.random.NextFloat4(max);
            rnd.Dispose();
            return result;
        }

    }

}