﻿using System;
using UnityEngine;


namespace Detonate
{
    [Serializable]
    public class ObstacleModule3D : FluidSimModule
    {
        public void SetBoundary(Vector3 _size, ComputeBuffer _obstacle_grid, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            int kernel_id = compute_shader.FindKernel("Boundary");
            compute_shader.SetBuffer(kernel_id, "write_R", _obstacle_grid);
            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
        }

    }
}
