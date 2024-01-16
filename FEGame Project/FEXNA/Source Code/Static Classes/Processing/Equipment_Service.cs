namespace FEXNA
{
    class Equipment_Service : FEXNA_Library.IEquipmentService
    {
        public FEXNA_Library.Data_Equipment equipment(FEXNA_Library.Item_Data data)
        {
            if (data.is_weapon)
            {
                if (Global.data_weapons.ContainsKey(data.Id))
                    return Global.data_weapons[data.Id];
            }
            else if (data.is_item)
            {
                if (Global.data_items.ContainsKey(data.Id))
                    return Global.data_items[data.Id];
            }
            return null;
        }
    }
}
