namespace ME.BECS {
    
    using Unity.Collections;

    public static class Constants {

        #if UNITY_2023_1_OR_NEWER
        public const Allocator ALLOCATOR_DOMAIN = Allocator.Domain;
        #else
        public static Allocator ALLOCATOR_DOMAIN => ALLOCATOR_PERSISTENT_ST.ToAllocator;
        #endif
        
        public const Allocator ALLOCATOR_PERSISTENT = Allocator.Persistent;
        public const Allocator ALLOCATOR_TEMP = Allocator.Temp;
        public const Allocator ALLOCATOR_TEMPJOB = Allocator.TempJob;

        public static AllocatorManager.AllocatorHandle ALLOCATOR_TEMP_ST => WorldsTempAllocator.allocatorTemp.Allocator.Handle;
        public static AllocatorManager.AllocatorHandle ALLOCATOR_PERSISTENT_ST => WorldsPersistentAllocator.allocatorPersistent.Allocator.Handle;

    }

}