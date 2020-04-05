using Clunker.Input;
using Clunker.Physics.Voxels;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Construct
{
    public class ConstructFlightControl : Component, IUpdateable, IComponentEventListener
    {
        private List<Thruster> _all;

        private List<Thruster> _posX;
        private List<Thruster> _negX;

        private List<Thruster> _posY;
        private List<Thruster> _negY;

        private List<Thruster> _posZ;
        private List<Thruster> _negZ;

        private List<Thruster> _posRotX;
        private List<Thruster> _negRotX;

        private List<Thruster> _posRotY;
        private List<Thruster> _negRotY;

        private List<Thruster> _posRotZ;
        private List<Thruster> _negRotZ;

        public ConstructFlightControl()
        {
            _all = new List<Thruster>();

            _posX = new List<Thruster>();
            _negX = new List<Thruster>();

            _posY = new List<Thruster>();
            _negY = new List<Thruster>();

            _posZ = new List<Thruster>();
            _negZ = new List<Thruster>();

            _posRotX = new List<Thruster>();
            _negRotX = new List<Thruster>();

            _posRotY = new List<Thruster>();
            _negRotY = new List<Thruster>();

            _posRotZ = new List<Thruster>();
            _negRotZ = new List<Thruster>();
        }

        public void ComponentStarted()
        {
            var body = GameObject.GetComponent<DynamicVoxelSpaceBody>();
            body.NewShapeGenerated += _body_NewShapeGenerated;
            _body_NewShapeGenerated();
        }

        private void _body_NewShapeGenerated()
        {
            _all.Clear();

            _posX.Clear();
            _negX.Clear();

            _posY.Clear();
            _negY.Clear();

            _posZ.Clear();
            _negZ.Clear();

            _posRotX.Clear();
            _negRotX.Clear();

            _posRotY.Clear();
            _negRotY.Clear();

            _posRotZ.Clear();
            _negRotZ.Clear();

            var space = GameObject.GetComponent<VoxelSpace>();
            var body = GameObject.GetComponent<DynamicVoxelSpaceBody>();
            var offset = body.LocalBodyOffset;

            foreach(var entity in space.VoxelEntities)
            {
                var thruster = entity.GetComponent<Thruster>();
                if(thruster != null)
                {
                    _all.Add(thruster);
                    var bodyPos = entity.Transform.Position - offset;
                    switch (thruster.Voxel.Orientation)
                    {
                        case VoxelSide.TOP:
                            _negY.Add(thruster);

                            if (IsLessThan(bodyPos.Z, 0)) _negRotX.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Z, 0)) _posRotX.Add(thruster);

                            if (IsLessThan(bodyPos.X, 0)) _posRotZ.Add(thruster);
                            else if (IsGreaterThan(bodyPos.X, 0)) _negRotZ.Add(thruster);

                            break;
                        case VoxelSide.BOTTOM:
                            _posY.Add(thruster);

                            if (IsLessThan(bodyPos.Z, 0)) _posRotX.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Z, 0)) _negRotX.Add(thruster);

                            if (IsLessThan(bodyPos.X, 0)) _negRotZ.Add(thruster);
                            else if (IsGreaterThan(bodyPos.X, 0)) _posRotZ.Add(thruster);
                            break;
                        case VoxelSide.NORTH:
                            _posZ.Add(thruster);

                            if (IsLessThan(bodyPos.Y, 0)) _negRotX.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Y, 0)) _posRotX.Add(thruster);

                            if (IsLessThan(bodyPos.X, 0)) _posRotY.Add(thruster);
                            else if (IsGreaterThan(bodyPos.X, 0)) _negRotY.Add(thruster);
                            break;
                        case VoxelSide.SOUTH:
                            _negZ.Add(thruster);

                            if (IsLessThan(bodyPos.Y, 0)) _posRotX.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Y, 0)) _negRotX.Add(thruster);

                            if (IsLessThan(bodyPos.X, 0)) _negRotY.Add(thruster);
                            else if (IsGreaterThan(bodyPos.X, 0)) _posRotY.Add(thruster);
                            break;
                        case VoxelSide.EAST:
                            _negX.Add(thruster);

                            if (IsLessThan(bodyPos.Z, 0)) _posRotY.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Z, 0)) _negRotY.Add(thruster);

                            if (IsLessThan(bodyPos.Y, 0)) _negRotZ.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Y, 0)) _posRotZ.Add(thruster);
                            break;
                        case VoxelSide.WEST:
                            _posX.Add(thruster);

                            if (IsLessThan(bodyPos.Z, 0)) _negRotY.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Z, 0)) _posRotY.Add(thruster);

                            if (IsLessThan(bodyPos.Y, 0)) _posRotZ.Add(thruster);
                            else if (IsGreaterThan(bodyPos.Y, 0)) _negRotZ.Add(thruster);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private bool IsLessThan(float num, float target)
        {
            return num < target - 0.05;
        }

        private bool IsGreaterThan(float num, float target)
        {
            return num > target + 0.05;
        }

        public void ComponentStopped()
        {
            var body = GameObject.GetComponent<DynamicVoxelSpaceBody>();
            body.NewShapeGenerated -= _body_NewShapeGenerated;
        }

        public void Update(float time)
        {
            _all.ForEach(t => t.IsFiring = false);
            if(GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad8))
            {
                _negZ.ForEach(t => t.IsFiring = true);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad2))
            {
                _posZ.ForEach(t => t.IsFiring = true);
            }

            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad4))
            {
                _negX.ForEach(t => t.IsFiring = true);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad6))
            {
                _posX.ForEach(t => t.IsFiring = true);
            }

            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad1))
            {
                _posRotZ.ForEach(t => t.IsFiring = true);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad3))
            {
                _negRotZ.ForEach(t => t.IsFiring = true);
            }

            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad7))
            {
                _posRotY.ForEach(t => t.IsFiring = true);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad9))
            {
                _negRotY.ForEach(t => t.IsFiring = true);
            }

            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad5))
            {
                _posY.ForEach(t => t.IsFiring = true);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.Keypad0))
            {
                _negY.ForEach(t => t.IsFiring = true);
            }

            if (GameInputTracker.IsKeyPressed(Veldrid.Key.KeypadMinus))
            {
                _negRotX.ForEach(t => t.IsFiring = true);
            }
            if (GameInputTracker.IsKeyPressed(Veldrid.Key.KeypadPlus))
            {
                _posRotX.ForEach(t => t.IsFiring = true);
            }
        }
    }
}
