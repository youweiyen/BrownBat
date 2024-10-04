using GH_IO;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;


namespace BrownBat.Components
{
    public class BitmapImage : IGH_Goo, GH_ISerializable
    {
        protected Bitmap bitmap = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        public BitmapImage(Bitmap bitmap) => this.bitmap = (Bitmap)bitmap.Clone();


        public bool IsValid => true;

        public string IsValidWhyNot => throw new NotImplementedException();

        public string TypeName => throw new NotImplementedException();

        public string TypeDescription => throw new NotImplementedException();

        public bool CastFrom(object source)
        {
            throw new NotImplementedException();
        }

        public bool CastTo<T>(out T target)
        {
            throw new NotImplementedException();
        }

        public IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public IGH_GooProxy EmitProxy()
        {
            throw new NotImplementedException();
        }

        public bool Read(GH_IReader reader)
        {
            throw new NotImplementedException();
        }

        public object ScriptVariable()
        {
            throw new NotImplementedException();
        }

        public bool Write(GH_IWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
