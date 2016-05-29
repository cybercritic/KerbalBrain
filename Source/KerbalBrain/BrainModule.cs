using UnityEngine;
using KIS;
using System.Collections.Generic;

namespace KerbalBrain
{
    public class BrainModule : PartModule
    {
        private bool activated = false;
        private VesselType originalType = VesselType.Unknown;
        private ModuleKISInventory.InventoryType originalInventory = ModuleKISInventory.InventoryType.Container;

        public override void OnStart(StartState state)
        {
            print("[KBrain] OnStart");
            base.OnStart(state);

            //KIS needs a skin with arms to attach objects to
            SkinnedMeshRenderer skin = this.part.gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.name = "body01";

            Transform rightArm = new GameObject().transform;
            rightArm.position = this.transform.position;
            rightArm.name = "bn_r_mid_a01";
            rightArm.parent = this.transform;

            Transform leftArm = new GameObject().transform;
            leftArm.position = this.transform.position;
            leftArm.name = "bn_l_mid_a01";
            leftArm.parent = this.transform;

            Transform[] bones = new Transform[2];
            bones[0] = rightArm;
            bones[1] = leftArm;

            skin.bones = bones;

            //set initial inventory
            ModuleKISInventory inventory = this.part.Modules.GetModule<ModuleKISInventory>();
            inventory.invType = this.originalInventory;
            inventory.enabled = true;
            inventory.invName = "Brain's";
            inventory.Events["ShowInventory"].active = true;

            //refresh part menu
            inventory.Events["ShowInventory"].guiActive = false;
            inventory.Events["ShowInventory"].guiActive = true;
        }

        //on/off switch for brain/eva mode
        [KSPField(guiActive = true, guiActiveEditor =true, guiName = "Brain Status", isPersistant = true), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool isActivated = false;
        
        public void BrainSwitch()
        {
            if (!activated)
            {
                //disable all other inventories, eva kerbal should only have one
                List<ModuleKISInventory> list = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISInventory>();
                foreach (ModuleKISInventory inv in list)
                    inv.Events["ShowInventory"].active = false;

                //get a engineer on-board
                ProtoCrewMember pcm = new ProtoCrewMember(ProtoCrewMember.KerbalType.Unowned);

                //adding "brain" to module, if one is not there already
                if (this.part.protoModuleCrew.Count == 0)
                {
                    //couldn't find a way to set the RepairSkill skill, asking for new kerbals until one has it
                    bool foundRepairSkill = false;
                    int abort = 0;
                    while (!foundRepairSkill)
                    {
                        pcm = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
                        pcm.name = "Brain";

                        //check for skill
                        foreach (var expEffect in pcm.experienceTrait.Effects)
                            if (expEffect.ToString().ToLower().IndexOf("repairskill") != -1)
                                foundRepairSkill = true;

                        //check so we don't loop indefinately
                        if(abort++ > 128)
                        {
                            print("[KBrain] Couldn't find a brain with RepairSkill, aborting.");
                            return;
                        }
                    }

                    pcm.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                    this.part.AddCrewmemberAt(pcm, 0);
                }

                //change to eva
                this.originalType = this.vessel.vesselType;
                this.vessel.vesselType = VesselType.EVA;

                //set inventory to eva
                ModuleKISInventory inventory = this.part.Modules.GetModule<ModuleKISInventory>();
                this.originalInventory = inventory.invType;

                inventory.invType = ModuleKISInventory.InventoryType.Eva;
                inventory.enabled = true;
                inventory.invName = "Brain's";
                inventory.Events["ShowInventory"].active = true;

                print("[KBrain] Vessel Eva: " + this.vessel.isEVA);
                
                this.activated = true;
            }
            else
            {
                //change back from eva
                this.part.vessel.vesselType = this.originalType;

                //set inventory back to what it was
                ModuleKISInventory inventory = this.part.Modules.GetModule<ModuleKISInventory>();
                inventory.invType = this.originalInventory;
                inventory.invName = "Brain's";
                inventory.Events["ShowInventory"].active = true;

                //enable all other inventories
                List<ModuleKISInventory> list = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISInventory>();
                foreach (ModuleKISInventory inv in list)
                    if (inv.part != this.part)//skip the other part inventory
                        inv.Events["ShowInventory"].active = true;
                
                this.activated = false;
            }
        }

        void LateUpdate()
        {
            if (isActivated != activated)
                this.BrainSwitch();
        }
    }
}

