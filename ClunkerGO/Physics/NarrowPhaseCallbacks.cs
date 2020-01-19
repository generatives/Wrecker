using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Clunker.Physics
{
    //The simulation has a variety of extension points that must be defined. 
    //The demos tend to reuse a few types like the DemoNarrowPhaseCallbacks, but this demo will provide its own (super simple) versions.
    //If you're wondering why the callbacks are interface implementing structs rather than classes or events, it's because 
    //the compiler can specialize the implementation using the compile time type information. That avoids dispatch overhead associated
    //with delegates or virtual dispatch and allows inlining, which is valuable for extremely high frequency logic like contact callbacks.
    unsafe struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        /// <summary>
        /// Performs any required initialization logic after the Simulation instance has been constructed.
        /// </summary>
        /// <param name="simulation">Simulation that owns these callbacks.</param>
        public void Initialize(Simulation simulation)
        {
            //Often, the callbacks type is created before the simulation instance is fully constructed, so the simulation will call this function when it's ready.
            //Any logic which depends on the simulation existing can be put here.
        }

        /// <summary>
        /// Chooses whether to allow contact generation to proceed for two overlapping collidables.
        /// </summary>
        /// <param name="workerIndex">Index of the worker that identified the overlap.</param>
        /// <param name="a">Reference to the first collidable in the pair.</param>
        /// <param name="b">Reference to the second collidable in the pair.</param>
        /// <returns>True if collision detection should proceed, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
        {
            //Before creating a narrow phase pair, the broad phase asks this callback whether to bother with a given pair of objects.
            //This can be used to implement arbitrary forms of collision filtering. See the RagdollDemo or NewtDemo for examples.
            return true;
        }

        /// <summary>
        /// Chooses whether to allow contact generation to proceed for the children of two overlapping collidables in a compound-including pair.
        /// </summary>
        /// <param name="pair">Parent pair of the two child collidables.</param>
        /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
        /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
        /// <returns>True if collision detection should proceed, false otherwise.</returns>
        /// <remarks>This is called for each sub-overlap in a collidable pair involving compound collidables. If neither collidable in a pair is compound, this will not be called.
        /// For compound-including pairs, if the earlier call to AllowContactGeneration returns false for owning pair, this will not be called. Note that it is possible
        /// for this function to be called twice for the same subpair if the pair has continuous collision detection enabled; 
        /// the CCD sweep test that runs before the contact generation test also asks before performing child pair tests.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            //This is similar to the top level broad phase callback above. It's called by the narrow phase before generating
            //subpairs between children in parent shapes. 
            //This only gets called in pairs that involve at least one shape type that can contain multiple children, like a Compound.
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ConfigureMaterial(out PairMaterialProperties pairMaterial)
        {
            //The engine does not define any per-body material properties. Instead, all material lookup and blending operations are handled by the callbacks.
            //For the purposes of this demo, we'll use the same settings for all pairs.
            //(Note that there's no bounciness property! See here for more details: https://github.com/bepu/bepuphysics2/issues/3)
            pairMaterial.FrictionCoefficient = 1f;
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = new SpringSettings(30, 1);
        }

        //Note that there is a unique callback for convex versus nonconvex types. There is no fundamental difference here- it's just a matter of convenience
        //to avoid working through an interface or casting.
        //For the purposes of the demo, contact constraints are always generated.
        /// <summary>
        /// Provides a notification that a manifold has been created for a pair. Offers an opportunity to change the manifold's details. 
        /// </summary>
        /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
        /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
        /// <param name="manifold">Set of contacts detected between the collidables.</param>
        /// <param name="pairMaterial">Material properties of the manifold.</param>
        /// <returns>True if a constraint should be created for the manifold, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, NonconvexContactManifold* manifold, out PairMaterialProperties pairMaterial)
        {
            ConfigureMaterial(out pairMaterial);
            return true;
        }

        /// <summary>
        /// Provides a notification that a manifold has been created for a pair. Offers an opportunity to change the manifold's details. 
        /// </summary>
        /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
        /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
        /// <param name="manifold">Set of contacts detected between the collidables.</param>
        /// <param name="pairMaterial">Material properties of the manifold.</param>
        /// <returns>True if a constraint should be created for the manifold, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, ConvexContactManifold* manifold, out PairMaterialProperties pairMaterial)
        {
            ConfigureMaterial(out pairMaterial);
            return true;
        }

        /// <summary>
        /// Provides a notification that a manifold has been created between the children of two collidables in a compound-including pair.
        /// Offers an opportunity to change the manifold's details. 
        /// </summary>
        /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
        /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
        /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
        /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
        /// <param name="manifold">Set of contacts detected between the collidables.</param>
        /// <returns>True if this manifold should be considered for constraint generation, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ConvexContactManifold* manifold)
        {
            return true;
        }

        /// <summary>
        /// Releases any resources held by the callbacks. Called by the owning narrow phase when it is being disposed.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
