namespace FEXNA
{
    public class Scene_Soft_Reset : Scene_Base
    {
        const int WAIT_TIME = 8;
        protected int Timer = WAIT_TIME;
        protected bool Loading;

        public Scene_Soft_Reset()
        {
            Scene_Type = "Scene_Soft_Reset";
            Global.load_save_info = true;
            Global.Battler_Content.Unload();
            Global.game_map = null;
        }

        public override void update()
        {
            if (Loading)
            {
                // If trying to load suspend and failed
                if (!Global.suspend_load_successful)
                {
                    Loading = false;
                }
                else
                {
                    Global.scene_change("Load_Suspend");
                    return;
                }
            }
            if (Timer <= 0)
            {
                if (Global.Input.pressed(Inputs.Start))
                {
                    if (Global.suspend_file_info != null)
                    {
                        Global.loading_suspend = true;
                        Loading = true;
                        return;
                    }
                    else
                    {
                        Global.scene_change("Scene_Title");
                        return;
                    }
                }
                Global.scene_change("Scene_Splash");
            }
            else
            {
                Timer--;
                if (Global.load_save_info || Global.Input.soft_reset())
                    Timer = WAIT_TIME;
            }
        }
    }
}
