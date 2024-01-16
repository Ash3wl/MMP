using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using ListExtension;
using ArrayExtension;

// TODO: replace this with the type you want to write out.
using TWrite = FEXNA_Library.Data_Terrain;

namespace LevelContentPipelineExtension
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class TerrainWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            // Id
            output.Write(value.Id);
            // Name
            output.Write(value.Name);
            // Avoid
            output.Write(value.Avoid);
            // Def
            output.Write(value.Def);
            // Res
            output.Write(value.Res);
            // Stats Visible
            output.Write(value.Stats_Visible);
            // Step Sound Group
            output.Write(value.Step_Sound_Group);
            // Platform Rename
            output.Write(value.Platform_Rename);
            // Background Rename
            output.Write(value.Background_Rename);
            // Dust Type
            output.Write(value.Dust_Type);
            // Can Fire Through
            output.Write(value.Fire_Through);
            // Move Costs
            value.Move_Costs.write(output);
            // Heal
            output.Write(value.Heal != null);
            if (value.Heal != null)
                value.Heal.write(output);
            // Minimap
            output.Write(value.Minimap);
            // Minimap Group
            value.Minimap_Group.write(output);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FEXNA_Library.TerrainReader).AssemblyQualifiedName;
        }
    }
}
