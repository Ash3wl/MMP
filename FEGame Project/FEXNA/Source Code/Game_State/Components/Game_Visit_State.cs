using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Vector2Extension;

namespace FEXNA.State
{
    public enum Visit_Modes { Visit, Chest, Door }
    class Game_Visit_State : Game_State_Component
    {
        protected bool Visit_Calling = false;
        protected bool In_Visit = false;
        internal bool Pillaging = false; //private // also all this other stuff private //Yeti
        protected int Visit_Action = 0;
        protected int Visit_Timer = 0;
        protected int Visitor_Id = -1;
        protected Vector2 Visit_Loc = new Vector2(-1, -1);
        protected int Visit_Mode;

        #region Serialization
        internal override void write(BinaryWriter writer)
        {
            writer.Write(Visit_Calling);
            writer.Write(In_Visit);
            writer.Write(Pillaging);
            writer.Write(Visit_Action);
            writer.Write(Visit_Timer);
            writer.Write(Visitor_Id);
            Visit_Loc.write(writer);
            writer.Write(Visit_Mode);
        }

        internal override void read(BinaryReader reader)
        {
            Visit_Calling = reader.ReadBoolean();
            In_Visit = reader.ReadBoolean();
            Pillaging = reader.ReadBoolean();
            Visit_Action = reader.ReadInt32();
            Visit_Timer = reader.ReadInt32();
            Visitor_Id = reader.ReadInt32();
            Visit_Loc = Visit_Loc.read(reader);
            Visit_Mode = reader.ReadInt32();
        }
        #endregion

        #region Accessors
        public bool visit_calling
        {
            get { return Visit_Calling; }
            set { Visit_Calling = value; }
        }

        public bool in_visit { get { return In_Visit; } }

        public Game_Unit visitor { get { return Visitor_Id == -1 ? null : Units[Visitor_Id]; } }

        public Vector2 visit_loc { get { return Visit_Loc; } }

        public int visit_mode
        {
            get { return Visit_Mode; }
            set { Visit_Mode = value; }
        }
        #endregion

        internal override void update()
        {
            if (Visit_Calling)
            {
                In_Visit = true;
                Visit_Calling = false;
            }
            if (In_Visit)
            {
                bool cont = false;
                while (!cont)
                {
                    cont = true;
                    switch (Visit_Action)
                    {
                        case 0:
                            switch (Visit_Timer)
                            {
                                case 0:
                                    if (Global.game_state.is_player_turn)
                                        Global.scene.suspend();
                                    Visitor_Id = Global.game_system.Visitor_Id;
                                    Visit_Loc = Global.game_system.Visit_Loc;
                                    Global.game_system.Visitor_Id = -1;
                                    Global.game_system.Visit_Loc = new Vector2(-1, -1);
                                    Visit_Timer++;
                                    break;
                                case 8:
                                    Visit_Action = 1;
                                    Visit_Timer = 0;
                                    break;
                                default:
                                    Visit_Timer++;
                                    break;
                            }
                            break;
                        // Loads Visit Event
                        case 1:
                            if (Visit_Mode == (int)Visit_Modes.Visit)
                                Global.game_map.activate_visit(Visit_Loc, Pillaging);
                            else if (Visit_Mode == (int)Visit_Modes.Chest)
                            {
                                visitor.use_chest_key();
                                Global.game_map.activate_chest(Visit_Loc);
                            }
                            else if (Visit_Mode == (int)Visit_Modes.Door)
                            {
                                visitor.use_door_key();
                                Global.game_map.activate_door(Visit_Loc);
                            }
                            Visit_Action = 2;
                            break;
                        case 2:
                            if (!Global.game_system.is_interpreter_running && !Global.scene.is_message_window_active)
                                Visit_Action = 3;
                            break;
                        case 3:
                            switch (Visit_Timer)
                            {
                                case 18:
                                    Visit_Action = 4;
                                    Visit_Timer = 0;
                                    break;
                                default:
                                    Visit_Timer++;
                                    break;
                            }
                            break;
                        case 4:
                            visitor.actor.staff_fix();
                            Visit_Action = 5;
                            break;
                        case 5:
                            if (visitor.cantoing && !Pillaging && visitor.is_active_player_team) //Multi
                                visitor.open_move_range();
                            else
                                visitor.start_wait(false);
                            visitor.queue_move_range_update();
                            refresh_move_ranges();
                            Visit_Action = 6;
                            break;
                        case 6:
                            if (!Global.game_system.is_interpreter_running && !Global.scene.is_message_window_active)
                            {
                                wait_for_move_update();
                                end_visit();
                            }
                            break;
                    }
                }
            }
        }

        protected void end_visit()
        {
            In_Visit = false;
            Pillaging = false;
            Visit_Action = 0;
            Visit_Timer = 0;
            Visitor_Id = -1;
            Visit_Loc = new Vector2(-1, -1);
            highlight_test();
        }
    }
}
