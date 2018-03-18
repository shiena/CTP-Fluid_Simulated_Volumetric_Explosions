﻿using System.Collections.Generic;
using UnityEngine;


namespace Detonate
{
    public class FluidSmoke3D : FluidSimulation3D
    {
        [SerializeField] FluidSmoke3DParams smoke_params = new FluidSmoke3DParams();

        [SerializeField] BuoyancyModule3D buoyancy_module = new BuoyancyModule3D();
        private ComputeBuffer[] density_grids = new ComputeBuffer[2];//smoke simulates movement of density
        [SerializeField] protected List<SmokeEmitter> emitters = new List<SmokeEmitter>();

        protected override void Start()
        {
            InitSim();
        }


        public override void ResetSim()
        {
            base.ResetSim();
            OnDestroy();//in case of reset
            InitSim();
        }


        protected override void InitSim()
        {
            base.InitSim();
            int buffer_size = sim_params.width * sim_params.height * sim_params.depth;
            CreateDensityGrids(buffer_size);
        }


        private void CreateDensityGrids(int _buffer_size)
        {
            density_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            density_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        protected override void Update()
        {
            base.Update();//determines which time step to use

            MoveStage();
            AddForcesStage();
            CalculateDivergence();//i.e. fluid diffusion
            MassConservationStage();
            CreateObstacles();
            UpdateVolumeRenderer();
        }


        protected override void MoveStage()
        {
            advection_module.ApplyAdvection(sim_dt, size, smoke_params.density_dissipation,
                density_grids, velocity_grids, obstacle_grid, thread_count);

            base.MoveStage();
        }


        private void AddForcesStage()
        {
            ApplyBuoyancy();
            ApplyEmitters();
        }


        private void ApplyBuoyancy()
        {
            buoyancy_module.ApplyBuoyancy(sim_dt, size, smoke_params.smoke_buoyancy,
                smoke_params.smoke_weight, sim_params.ambient_temperature,
                velocity_grids, density_grids, temperature_grids, thread_count);
        }


        public List<SmokeEmitter> Emitters
        {
            get
            {
                return emitters;
            }
            set
            {
                emitters = value;
            }
        }


        private void ApplyEmitters()
        {
            for(int i = 0; i < emitters.Count; ++i)
            {
                if (emitters[i] == null)
                {
                    emitters.RemoveAt(i);
                    continue;
                }

                if (!emitters[i].isActiveAndEnabled)
                    continue;

                if (!emitters[i].gameObject.activeInHierarchy)
                    continue;

                if (!emitters[i].Emit)
                    continue;

                ApplyImpulse(emitters[i].DenisityAmount, emitters[i].EmissionRadius,
                    density_grids, emitters[i].transform.position);
                ApplyImpulse(emitters[i].TemperatureAmount, emitters[i].EmissionRadius,
                    temperature_grids, emitters[i].transform.position);
            }
        }


        protected override void ConvertGridToVolume(GridType _grid_type)
        {
            if (_grid_type == GridType.DENSITY)
            {
                output_module.ConvertToVolume(size, density_grids[READ], volume_output, thread_count);
                return;
            }

            base.ConvertGridToVolume(_grid_type);
        }
     

        //all buffers should be released on destruction
        protected override void OnDestroy()
        {
            base.OnDestroy();
            density_grids[READ].Release();
            density_grids[WRITE].Release();        
        }
    }
}
