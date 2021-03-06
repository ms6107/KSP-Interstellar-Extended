﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Microwave
{
    class BandwidthConverter : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Wavelength Name")]
        public string bandwidthName = "missing";
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Target Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double targetWavelength = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Start Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double minimumWavelength = 0.001;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "End Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double maximumWavelength = 1.000;

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement")]
        public int AvailableTechLevel;

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Efficiency Old", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage0 = 45;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage0 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Thermal Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage0 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement")]
        public string techRequirement0 = "";

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Efficiency Old", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage1 = 45;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage1 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Thermal Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage1 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement")]
        public string techRequirement1 = "";

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Efficiency Old", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage2 = 45;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage2 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Thermal Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage2 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement")]
        public string techRequirement2 = "";


        public void Initialize()
        {
            if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement2))
            {
                AvailableTechLevel = 2;
            }
            if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement1))
            {
                AvailableTechLevel = 1;
            }
            else if (PluginHelper.HasTechRequirementOrEmpty(techRequirement0))
            {
                AvailableTechLevel = 0;
            }
        }

        public double MaxElectricEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return electricEfficiencyPercentage0 > 0 ? electricEfficiencyPercentage0 : efficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return electricEfficiencyPercentage1 > 0 ? electricEfficiencyPercentage1 : efficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return electricEfficiencyPercentage2 > 0 ? electricEfficiencyPercentage2 : efficiencyPercentage2;
                else
                    return 0;
            }
        }

        public double MaxThermalEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return thermalEfficiencyPercentage0 > 0 ? thermalEfficiencyPercentage0 : efficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return thermalEfficiencyPercentage1 > 0 ? thermalEfficiencyPercentage1 : efficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return thermalEfficiencyPercentage2 > 0 ? thermalEfficiencyPercentage2 : efficiencyPercentage2;
                else
                    return 0;
            }
        }

        public double MaxEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return efficiencyPercentage0 > 0 ? efficiencyPercentage0 : thermalEfficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return efficiencyPercentage1 > 0 ? efficiencyPercentage1 : thermalEfficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return efficiencyPercentage2 > 0 ? efficiencyPercentage2 : thermalEfficiencyPercentage2;
                else
                    return 0;
            }
        }


        public double TargetWavelength
        {
            get 
            {
                if (targetWavelength == 0)
                    targetWavelength = (minimumWavelength + maximumWavelength) / 2;
                
                return targetWavelength;
            }
        }

    }


}
