﻿using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Vector2Extension;

namespace FEXNA_Library
{
    public class Face_Data : IFEXNADataContent
    {
        public string Name;
        public int Emotions = 0;
        public int Pitch = 100;
        public Vector2 EyesOffset = new Vector2(24, 24);
        public Vector2 MouthOffset = new Vector2(24, 40);
        public Vector2 StatusOffset = new Vector2(8, 0);
        public int StatusFrame = 0;
        public int PlacementOffset = 0;
        public bool ForceEyesClosed = false;
        public bool ClassCard = false;

        public IFEXNADataContent Read_Content(ContentReader input)
        {
            Face_Data result = new Face_Data();

            result.Name = input.ReadString();
            result.Emotions = input.ReadInt32();
            result.Pitch = input.ReadInt32();
            result.EyesOffset = EyesOffset.read(input);
            result.MouthOffset = MouthOffset.read(input);
            result.StatusOffset = StatusOffset.read(input);
            result.StatusFrame = input.ReadInt32();
            result.PlacementOffset = input.ReadInt32();
            result.ForceEyesClosed = input.ReadBoolean();
            result.ClassCard = input.ReadBoolean();

            return result;
        }
        public void Write(BinaryWriter output)
        {
            output.Write(Name);
            output.Write(Emotions);
            output.Write(Pitch);
            EyesOffset.write(output);
            MouthOffset.write(output);
            StatusOffset.write(output);
            output.Write(StatusFrame);
            output.Write(PlacementOffset);
            output.Write(ForceEyesClosed);
            output.Write(ClassCard);
        }

        public Face_Data() { }
        public Face_Data(Face_Data other)
        {
            Name = other.Name;
            Emotions = other.Emotions;
            Pitch = other.Pitch;
            EyesOffset = other.EyesOffset;
            MouthOffset = other.MouthOffset;
            StatusOffset = other.StatusOffset;
            StatusFrame = other.StatusFrame;
            PlacementOffset = other.PlacementOffset;
            ForceEyesClosed = other.ForceEyesClosed;
            ClassCard = other.ClassCard;
        }

        public override string ToString()
        {
            return string.Format("Face_Data: {0}", Name);
        }
    }
}