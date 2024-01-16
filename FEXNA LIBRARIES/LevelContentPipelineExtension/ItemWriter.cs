using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using ArrayExtension;
using ListExtension;

using TWrite = FEXNA_Library.Data_Item;

namespace LevelContentPipelineExtension
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class ItemWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            value.write(output);
            /*// Id
            output.Write(value.Id);
            // Name
            output.Write(value.Name);
            // Full Name
            output.Write(value.Full_Name);
            // Description
            output.Write(value.Description);
            // Quick Description
            output.Write(value.Quick_Desc);
            // Image Name
            output.Write(value.Image_Name);
            // Image Index
            output.Write(value.Image_Index);
            // Uses
            output.Write(value.Uses);
            // Cost
            output.Write(value.Cost);
            // Prf by Character
            value.Prf_Character.write(output);
            // Prf by Class
            value.Prf_Class.write(output);
            // Prf by Type
            value.Prf_Type.write(output);
            // Skills
            value.Skills.write(output);
            // Heal Val
            output.Write(value.Heal_Val);
            // Heal Percent
            output.Write((double)value.Heal_Percent);
            // Door Key
            output.Write(value.Door_Key);
            // Chest Key
            output.Write(value.Chest_Key);
            // Dancer Ring
            output.Write(value.Dancer_Ring);
            // Torch Radius
            output.Write(value.Torch_Radius);
            // Places Something
            output.Write((int)value.Placeable);
            // Repair Rate
            output.Write((double)value.Repair_Rate);
            // Boost Text
            output.Write(value.Boost_Text);
            // Stat Boost
            value.Stat_Boost.write(output);
            // Growth Boost
            value.Growth_Boost.write(output);
            // Stat Buff
            value.Stat_Buff.write(output);
            // Status Inflict
            value.Status_Inflict.write(output);
            // Status Remove
            value.Status_Remove.write(output);
            // Promotes
            value.Promotes.write(output);
            // Can Sell
            output.Write(value.Can_Sell);*/
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FEXNA_Library.ItemReader).AssemblyQualifiedName;
        }
    }
}
