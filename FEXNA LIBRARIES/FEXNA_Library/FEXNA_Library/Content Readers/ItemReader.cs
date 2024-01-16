using Microsoft.Xna.Framework.Content;
using ArrayExtension;
using ListExtension;

using TRead = FEXNA_Library.Data_Item;

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
    public class ItemReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            existingInstance = Data_Item.read(input);
            /*existingInstance = new TRead();
            // Id
            existingInstance.Id = input.ReadInt32();
            // Name
            existingInstance.Name = input.ReadString();
            // Full Name
            existingInstance.Full_Name = input.ReadString();
            // Description
            existingInstance.Description = input.ReadString();
            // Quick Description
            existingInstance.Quick_Desc = input.ReadString();
            // Image Name
            existingInstance.Image_Name = input.ReadString();
            // Image Index
            existingInstance.Image_Index = input.ReadInt32();
            // Uses
            existingInstance.Uses = input.ReadInt32();
            // Cost
            existingInstance.Cost = input.ReadInt32();
            // Prf by Character
            existingInstance.Prf_Character.read(input);
            // Prf by Class
            existingInstance.Prf_Class.read(input);
            // Prf by Type
            existingInstance.Prf_Type.read(input);
            // Skills
            existingInstance.Skills.read(input);
            // Heal Val
            existingInstance.Heal_Val = input.ReadInt32();
            // Heal Percent
            existingInstance.Heal_Percent = (float)input.ReadDouble();
            // Door Key
            existingInstance.Door_Key = input.ReadBoolean();
            // Chest Key
            existingInstance.Chest_Key = input.ReadBoolean();
            // Dancer Ring
            existingInstance.Dancer_Ring = input.ReadBoolean();
            // Torch Radius
            existingInstance.Torch_Radius = input.ReadInt32();
            // Places Something
            existingInstance.Placeable = (Placeables)input.ReadInt32();
            // Repair Rate
            existingInstance.Repair_Percent = (float)input.ReadDouble();
            // Boost Text
            existingInstance.Boost_Text = input.ReadString();
            // Stat Boost
            existingInstance.Stat_Boost = existingInstance.Stat_Boost.read(input);
            // Growth Boost
            existingInstance.Growth_Boost = existingInstance.Growth_Boost.read(input);
            // Stat Buff
            existingInstance.Stat_Buff = existingInstance.Stat_Buff.read(input);
            // Status Inflict
            existingInstance.Status_Inflict.read(input);
            // Status Remove
            existingInstance.Status_Remove.read(input);
            // Promotes
            existingInstance.Promotes.read(input);
            // Can Sell
            existingInstance.Can_Sell = input.ReadBoolean();*/

            return existingInstance;
        }
    }
}
