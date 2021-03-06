﻿using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    [KSPModule("Fission Reactor")]
    class InterstellarFissionNTR : InterstellarFissionMSRGC { }


    [KSPModule("Fission Reactor")]
    class InterstellarFissionMSRGC : InterstellarReactor, INuclearFuelReprocessable
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Actinides Modifier")]
        public double actinidesModifer;

        PartResourceDefinition fluorineGasDefinition;
        PartResourceDefinition depletedFuelDefinition;
        PartResourceDefinition enrichedUraniumDefinition;
        PartResourceDefinition oxygenGasDefinition;

        double fluorineDepletedFuelVolumeMultiplier;
        double enrichedUraniumVolumeMultiplier;
        double depletedToEnrichVolumeMultplier;
        double oxygenDepletedUraniumVolumeMultipler;
             
        public double WasteToReprocess { get { return part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides) ? part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount : 0; } }

        [KSPEvent(guiName = "Swap Fuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.5f)]
        public void SwapFuelMode()
        {
            if (!part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides) || part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount > 0.01) return;

            defuelCurrentFuel();
            if (IsCurrentFuelDepleted())
            {
                fuel_mode++;
                if (fuel_mode >= fuel_modes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuel_modes[fuel_mode];
                fuelModeStr = CurrentFuelMode.ModeGUIName;
                Refuel();
            }
        }

        [KSPEvent(guiName = "Swap Fuel", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = false)]
        public void EditorSwapFuel()
        {
            if (fuel_modes.Count == 1)
                return;

            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                part.Resources[fuel.ResourceName].amount = 0;
            }

            var startFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            var currentFirstFuelType = startFirstFuelType;

            do
            {
                fuel_mode++;
                if (fuel_mode >= fuel_modes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuel_modes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName == startFirstFuelType.ResourceName);

            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                part.Resources[fuel.ResourceName].amount = part.Resources[fuel.ResourceName].maxAmount;
            }

            fuelModeStr = CurrentFuelMode.ModeGUIName;
        }

        [KSPEvent(guiName = "Switch Mode", guiActiveEditor = true, guiActiveUnfocused = true, guiActive = true)]
        public void SwitchMode()
        {
            var startFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            var currentFirstFuelType = startFirstFuelType;

            // repeat until found same or differnt fuelmode with same kind of primary fuel
            do
            {
                fuel_mode++;
                if (fuel_mode >= fuel_modes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuel_modes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName != startFirstFuelType.ResourceName);

            fuelModeStr = CurrentFuelMode.ModeGUIName;
        }

        [KSPEvent(guiName = "Manual Restart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualRestart()
        {
            // verify any of the fuel types has at least 50% avaialbility inside the reactor
            if (CurrentFuelMode.Variants.Any(variant => variant.ReactorFuels.All(fuel => GetLocalResourceRatio(fuel) > 0.5)))
                IsEnabled = true;
        }

        [KSPEvent(guiName = "Manual Shutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        [KSPEvent(guiName = "Refuel", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]
        public void Refuel()
        {
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                if (!part.Resources.Contains(fuel.ResourceName) || !part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides)) return; // avoid exceptions, just in case
                PartResource fuel_reactor = part.Resources[fuel.ResourceName];
                PartResource actinides_reactor = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides];
                List<PartResource> fuel_resources = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == fuel.ResourceName)).ToList();

                double spare_capacity_for_fuel = fuel_reactor.maxAmount - actinides_reactor.amount;
                fuel_resources.ForEach(res =>
                {
                    double resource_available = res.amount;
                    double resource_added = Math.Min(resource_available, spare_capacity_for_fuel);
                    fuel_reactor.amount += resource_added;
                    res.amount -= resource_added;
                    spare_capacity_for_fuel -= resource_added;
                });
            }
        }

        public override bool IsFuelNeutronRich { get { return !CurrentFuelMode.Aneutronic; } }

        public override bool IsNuclear { get { return true; } }

        public override double MaximumThermalPower
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return base.MaximumThermalPower;

                if (CheatOptions.UnbreakableJoints)
                {
                    actinidesModifer = 1;
                    return base.MaximumThermalPower;
                }

                if (part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides) && part.Resources[InterstellarResourcesConfiguration.Instance.Actinides] != null)
                {
                    // get total amount of all fuels
                    double fuel_mass = CurrentFuelMode.Variants.Sum(m => m.ReactorFuels.Sum(fuel => GetLocalResourceAmount(fuel) * fuel.DensityInTon));

                    double actinide_mass = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount;
                    double fuel_actinide_mass_ratio = Math.Min(fuel_mass / (actinide_mass * CurrentFuelMode.NormalisedReactionRate * CurrentFuelMode.NormalisedReactionRate * CurrentFuelMode.NormalisedReactionRate * 2.5), 1.0);
                    fuel_actinide_mass_ratio = (double.IsInfinity(fuel_actinide_mass_ratio) || double.IsNaN(fuel_actinide_mass_ratio)) ? 1.0 : fuel_actinide_mass_ratio;
                    actinidesModifer = Math.Sqrt(fuel_actinide_mass_ratio);
                    return base.MaximumThermalPower * actinidesModifer;
                }
                return base.MaximumThermalPower;
            }
        }

        public override double CoreTemperature
        {
            get
            {
                if (!CheatOptions.IgnoreMaxTemperature && HighLogic.LoadedSceneIsFlight && !isupgraded)
                {
                    double temp_scale;

                    if (vessel != null && FNRadiator.hasRadiatorsForVessel(vessel))
                        temp_scale = FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
                    else
                        temp_scale = base.CoreTemperature / 2.0;

                    double temp_diff = (base.CoreTemperature - temp_scale) * Math.Sqrt(powerPcnt / 100.0);
                    return temp_scale + temp_diff;
                }
                else
                    return base.CoreTemperature;
            }
        }

        public override void OnUpdate()
        {
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            Events["Refuel"].active = Events["Refuel"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["Refuel"].guiName = "Refuel " + (CurrentFuelMode != null ? CurrentFuelMode.ModeGUIName : "");
            Events["SwapFuelMode"].active = Events["SwapFuelMode"].guiActiveUnfocused = fuel_modes.Count > 1 && !IsEnabled && !decay_ongoing;

            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = fuel_modes.Count > 1;
            Events["SwapFuelMode"].guiActive = Events["SwapFuelMode"].guiActiveUnfocused = fuel_modes.Count > 1;
            Events["EditorSwapFuel"].guiActiveEditor = Events["EditorSwapFuel"].guiActiveUnfocused = fuel_modes.Count > 1;

            base.OnUpdate();
        }

        public override void OnStart(PartModule.StartState state)
        {
            // start as normal
            base.OnStart(state);

            // auto switch if current fuel mode is depleted
            if (IsCurrentFuelDepleted())
            {
                fuel_mode++;
                if (fuel_mode >= fuel_modes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuel_modes[fuel_mode];
            }
            fuelModeStr = CurrentFuelMode.ModeGUIName;

            oxygenGasDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.OxygenGas);
            fluorineGasDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.FluorineGas);
            depletedFuelDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.DepletedFuel);
            enrichedUraniumDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.EnrichedUrarium);

            depletedToEnrichVolumeMultplier = enrichedUraniumDefinition.density / depletedFuelDefinition.density;
            fluorineDepletedFuelVolumeMultiplier = ((19 * 4) / 232d) * (depletedFuelDefinition.density / fluorineGasDefinition.density);
            enrichedUraniumVolumeMultiplier = (232d / (16 * 2 + 232d)) * (depletedFuelDefinition.density / enrichedUraniumDefinition.density);
            oxygenDepletedUraniumVolumeMultipler = ((16 * 2) / (16 * 2 + 232d)) * (depletedFuelDefinition.density / oxygenGasDefinition.density);

            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = fuel_modes.Count > 1;
            Events["SwapFuelMode"].guiActive = Events["SwapFuelMode"].guiActiveUnfocused = fuel_modes.Count > 1;
            Events["EditorSwapFuel"].guiActiveEditor = Events["EditorSwapFuel"].guiActiveUnfocused = fuel_modes.Count > 1;
        }

        public override void OnFixedUpdate()
        {
            // if reactor is overloaded with actinides, stop functioning
            if (IsEnabled && part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides))
            {
                if (part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount >= part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].maxAmount)
                {
                    part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].maxAmount;
                    IsEnabled = false;
                }
            }
            base.OnFixedUpdate();
        }

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }

        public double ReprocessFuel(double rate)
        {
            if (part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides))
            {
                PartResource actinides = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides];
                double new_actinides_amount = Math.Max(actinides.amount - rate, 0);
                double actinides_change = actinides.amount - new_actinides_amount;
                actinides.amount = new_actinides_amount;

                double depleted_fuels_request = actinides_change * 0.2;

                //depleted_fuels_change = -ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.DepletedFuel, -depleted_fuels_change);
                double depleted_fuels_produced = -Part.RequestResource(depletedFuelDefinition.id, -depleted_fuels_request, ResourceFlowMode.STAGE_PRIORITY_FLOW);

                // first try to replace depletedfuel with enriched uranium
                double enrichedUraniumRequest = depleted_fuels_produced * enrichedUraniumVolumeMultiplier;
                double enrichedUraniumRetrieved = Part.RequestResource(enrichedUraniumDefinition.id, enrichedUraniumRequest, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                double receivedEnrichedUraniumFraction = enrichedUraniumRequest > 0 ? enrichedUraniumRetrieved / enrichedUraniumRequest : 0;

                // if missing fluorine is dumped
                double oxygenChange = -Part.RequestResource(oxygenGasDefinition.id, -depleted_fuels_produced * oxygenDepletedUraniumVolumeMultipler * receivedEnrichedUraniumFraction, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                double fluorineChange = -Part.RequestResource(fluorineGasDefinition.id, -depleted_fuels_produced * fluorineDepletedFuelVolumeMultiplier * (1 - receivedEnrichedUraniumFraction), ResourceFlowMode.STAGE_PRIORITY_FLOW);

                var reactorFuels = CurrentFuelMode.Variants.First().ReactorFuels;

                double sum_useage_per_mw = reactorFuels.Sum(fuel => fuel.AmountFuelUsePerMJ * fuelUsePerMJMult);

                foreach (ReactorFuel fuel in reactorFuels)
                {
                    PartResource fuel_resource = part.Resources[fuel.ResourceName];

                    double powerFraction = sum_useage_per_mw > 0.0 ? fuel.AmountFuelUsePerMJ * fuelUsePerMJMult / sum_useage_per_mw : 1;

                    double new_fuel_amount = Math.Min(fuel_resource.amount + ((depleted_fuels_produced * 4) + (depleted_fuels_produced * receivedEnrichedUraniumFraction)) * powerFraction * depletedToEnrichVolumeMultplier, fuel_resource.maxAmount);
                    fuel_resource.amount = new_fuel_amount;
                }

                return actinides_change;
            }
            return 0;
        }

        // This Methods loads the correct fuel mode
        protected override void setDefaultFuelMode()
        {
            CurrentFuelMode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
        }

        private void defuelCurrentFuel()
        {
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                PartResource fuel_reactor = part.Resources[fuel.ResourceName];
                List<PartResource> swap_resource_list = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == fuel.ResourceName)).ToList();

                swap_resource_list.ForEach(res =>
                {
                    double spare_capacity_for_fuel = res.maxAmount - res.amount;
                    double fuel_added = Math.Min(fuel_reactor.amount, spare_capacity_for_fuel);
                    fuel_reactor.amount -= fuel_added;
                    res.amount += fuel_added;
                });
            }
        }

        private bool IsCurrentFuelDepleted()
        {
            return CurrentFuelMode.Variants.First().ReactorFuels.Any(fuel => GetFuelAvailability(fuel) < 0.001);
        }

    }
}