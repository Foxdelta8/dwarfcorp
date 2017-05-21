﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class MineSand : Goal
    {
        public int Counter = 0;
        private Gum.Widget CustomGUI;

        public MineSand()
        {
            Name = "Mine Sand";
            Description = "Mine 10 Sand";
            GoalType = GoalTypes.AvailableAtStartup;
        }

        public override void BuildCustomGUI(Widget Widget)
        {
            base.BuildCustomGUI(Widget);

            CustomGUI = Widget;
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            if (State == GoalState.Complete) return;
            if (Event is Events.DigBlock && (Event as Events.DigBlock).VoxelType.Name == "Sand")
            {
                Counter += 1;
                if (Counter == 10)
                {
                    World.MakeAnnouncement(Description + "\nSuccessfully met sand mining goal!");
                    State = GoalState.Complete;
                }

                if (CustomGUI != null) CustomGUI.Text = Description + "\n" + ((Counter >= 10) ? "Goal met!" : String.Format("{0} of 10 mined.", Counter));
            }
        }

    }
}