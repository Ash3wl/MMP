using Microsoft.Xna.Framework.Content;
using ListExtension;
using ArrayExtension;

using TRead = FEXNA_Library.Data_Terrain;

namespace FEXNA_Library
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content
    /// Pipeline to read the specified data type from binary .xnb format.
    /// 
    /// Unlike the other Content Pipeline support classes, this should
    /// be a part of your main game project, and not the Content Pipeline
    /// Extension Library project.
    /// </summary>
    public class TerrainReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            existingInstance = new TRead();
            // Id
            existingInstance.Id = input.ReadInt32();
            // Name
            existingInstance.Name = input.ReadString();
            // Avoid
            existingInstance.Avoid = input.ReadInt32();
            // Def
            existingInstance.Def = input.ReadInt32();
            // Res
            existingInstance.Res = input.ReadInt32();
            // Stats Visible
            existingInstance.Stats_Visible = input.ReadBoolean();
            // Step Sound Group
            existingInstance.Step_Sound_Group = input.ReadInt32();
            // Platform Rename
            existingInstance.Platform_Rename = input.ReadString();
            // Background Rename
            existingInstance.Background_Rename = input.ReadString();
            // Dust Type
            existingInstance.Dust_Type = input.ReadInt32();
            // Can Fire Through
            existingInstance.Fire_Through = input.ReadBoolean();
            // Move Costs
            existingInstance.Move_Costs = existingInstance.Move_Costs.read(input);
            // Heal
            if (input.ReadBoolean())
                existingInstance.Heal = existingInstance.Heal.read(input);
            // Minimap
            existingInstance.Minimap = input.ReadInt32();
            // Minimap Group
            existingInstance.Minimap_Group.read(input);

            return existingInstance;
        }
    }
}
