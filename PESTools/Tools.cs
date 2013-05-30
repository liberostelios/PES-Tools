using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Drawing;
using MiscUtil.IO;
using zlib;

namespace PESTool
{
    public struct _offsets
    {
        public uint offset_vert;
        public uint cant_vert;
        public byte nose1;
        public byte magic;
        public short nose2;
        public uint nose3;
        public uint nose4;
        public uint nose5;
        public uint nose6;
        public uint nose7;
        public uint offset_ind;
        public uint cant_ind;
    }

    public struct _xyz
    {
        public float x;
        public float y;
        public float z;
        public float tu;
        public float tv;
    }

    public struct triang
    {
        public short x;
        public short y;
        public short z;
    }

    public struct _vertex
    {
        public float x;
        public float y;
        public float z;
        public float nx;
        public float ny;
        public float nz;
        public float u;
        public float v;
        public float u2;
        public float v2;
        public Color vpaint;

        public void add(string[] stable)
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            x = System.Convert.ToSingle(stable[0], nfi);
            y = System.Convert.ToSingle(stable[1], nfi);
            z = System.Convert.ToSingle(stable[2], nfi);
            u = System.Convert.ToSingle(stable[3], nfi);
            v = System.Convert.ToSingle(stable[4], nfi);
            vpaint = hextocolor(stable[5]);
            nx = System.Convert.ToSingle(stable[6], nfi);
            ny = System.Convert.ToSingle(stable[7], nfi);
            nz = System.Convert.ToSingle(stable[8], nfi);
        }

        private Color hextocolor(string hexstring)
        {
            Color result;
            int ai, ri, gi, bi;

            ai = int.Parse(hexstring.Substring(2, 2), NumberStyles.HexNumber);
            ri = int.Parse(hexstring.Substring(4, 2), NumberStyles.HexNumber);
            gi = int.Parse(hexstring.Substring(6, 2), NumberStyles.HexNumber);
            bi = int.Parse(hexstring.Substring(8, 2), NumberStyles.HexNumber);

            result = Color.FromArgb(ai, ri, gi, bi);

            return result;
        }
    }

    public struct _triangle
    {
        public int x;
        public int y;
        public int z;

        public void add(string[] stable)
        {
            x = System.Convert.ToInt32(stable[0]);
            y = System.Convert.ToInt32(stable[1]);
            z = System.Convert.ToInt32(stable[2]);
        }
    }

    public enum OBJFORMAT
    {
        SINGLE = 0,
        LIGHTMAP = 1
    }

    public struct _object
    {
        public string textid;
        public int vertcount;
        public int indcount;
        public OBJFORMAT format;
        public _vertex center;
        public _vertex[] verts;
        public _triangle[] indices;
    }

    public struct _mesh
    {
        public int objcount;
        public string name;
        public _vertex max;
        public _vertex min;
        public _object[] objs;
        public bool twosides;

        public void calculatebounds()
        {
            max.x = objs[0].verts[0].x;
            max.y = objs[0].verts[0].y;
            max.z = objs[0].verts[0].z;
            min.x = objs[0].verts[0].x;
            min.y = objs[0].verts[0].y;
            min.z = objs[0].verts[0].z;

            for (int i = 0; i < objs.Length; i++)
            {
                for (int j = 0; j < objs[i].vertcount; j++)
                {
                    if (objs[i].verts[j].x > max.x)
                    {
                        max.x = objs[i].verts[j].x;
                    }
                    else if (objs[i].verts[j].x < min.x)
                    {
                        min.x = objs[i].verts[j].x;
                    }

                    if (objs[i].verts[j].y > max.y)
                    {
                        max.y = objs[i].verts[j].y;
                    }
                    else if (objs[i].verts[j].y < min.y)
                    {
                        min.y = objs[i].verts[j].y;
                    }

                    if (objs[i].verts[j].z > max.z)
                    {
                        max.z = objs[i].verts[j].z;
                    }
                    else if (objs[i].verts[j].z < min.z)
                    {
                        min.z = objs[i].verts[j].z;
                    }
                }
            }
        }
    }

    public struct _texture
    {
        public short id;
        public string file;
    }

    public struct _simplevert
    {
        public float x;
        public float y;
        public float z;

        public void add(string[] stable)
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            x = System.Convert.ToSingle(stable[0], nfi);
            y = System.Convert.ToSingle(stable[1], nfi);
            z = System.Convert.ToSingle(stable[2], nfi);
        }
    }

    public struct _stadium
    {
        public _mesh[] meshes;
        public _texture[] textures;
        public _crowd crowd;
    }

    public struct _quad
    {
        public _simplevert[] verts;
    }

    public struct _side
    {
        public _quad[] quads;
        public _simplevert center;
        public _simplevert space;
        public _simplevert lookat;

        private void centercalc()
        {
            center.x = 0;
            center.y = 0;
            center.z = 0;
            for (int i = 0; i < quads.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    center.x += quads[i].verts[j].x / (quads.Length * 4);
                    center.y += quads[i].verts[j].y / (quads.Length * 4);
                    center.z += quads[i].verts[j].z / (quads.Length * 4);
                }
            }
        }

        private void spacecalc()
        {
            float maxx, maxy, maxz, minx, miny, minz;

            maxx = quads[0].verts[0].x;
            maxy = quads[0].verts[0].y;
            maxz = quads[0].verts[0].z;
            minx = quads[0].verts[0].x;
            miny = quads[0].verts[0].y;
            minz = quads[0].verts[0].z;

            for (int i = 0; i < quads.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (quads[i].verts[j].x > maxx)
                    {
                        maxx = quads[i].verts[j].x;
                    }
                    else if (quads[i].verts[j].x < minx)
                    {
                        minx = quads[i].verts[j].x;
                    }

                    if (quads[i].verts[j].y > maxy)
                    {
                        maxy = quads[i].verts[j].y;
                    }
                    else if (quads[i].verts[j].y < miny)
                    {
                        miny = quads[i].verts[j].y;
                    }

                    if (quads[i].verts[j].z > maxz)
                    {
                        maxz = quads[i].verts[j].z;
                    }
                    else if (quads[i].verts[j].z < minz)
                    {
                        minz = quads[i].verts[j].z;
                    }
                }
            }

            space.x = Math.Abs(center.x - minx);
            if (Math.Abs(center.x - maxx) > space.x)
                space.x = Math.Abs(center.x - maxx);

            space.y = Math.Abs(center.y - miny);
            if (Math.Abs(center.y - maxy) > space.y)
                space.y = Math.Abs(center.y - maxy);

            space.z = Math.Abs(center.z - minz);
            if (Math.Abs(center.z - maxz) > space.z)
                space.z = Math.Abs(center.z - maxz);

            space.x = (float)(space.x * 1.2);
            space.y = (float)(space.y * 1.2);
            space.z = (float)(space.z * 1.2);
        }

        public void calculateparameters()
        {
            float temp;

            centercalc();
            spacecalc();

            temp = (float)System.Math.Sqrt((double)((Math.Pow(center.x, 2) + Math.Pow(center.y, 2))));
            lookat.x = -center.x / temp;
            lookat.y = -center.y / temp;
        }
    }

    public struct _crowd
    {
        public _side[] sides;
        public bool hascrowd;
    }

    public class Tools
    {
        public string[] fnames;
        public bool hasnames;

        public static void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }

        private string loadstring(Stream thestream)
        {
            string result;
            byte[] newchar = new byte[1];
            newchar[0] = (byte)thestream.ReadByte();
            result = "";
            while (newchar[0] != 0)
            {
                result += System.Text.Encoding.UTF8.GetString(newchar);
                newchar[0] = (byte)thestream.ReadByte();
            }
            return result;
        }

        public MemoryStream memorydecompress(byte[] inarray)
        {
            MemoryStream instream = new MemoryStream(inarray);
            MemoryStream unzlibed = new MemoryStream();

            ZOutputStream zlibstream = new ZOutputStream(unzlibed);

            byte[] buffer = new byte[inarray.Length];
            instream.Read(buffer, 0, buffer.Length);
            zlibstream.Write(buffer, 0, buffer.Length);

            return unzlibed;
        }

        public void decompresstoMemory(string inFile, MemoryStream memorystream)
        {
            int zeros = 0;

            FileStream instream = new FileStream(inFile, FileMode.Open);
            EndianBinaryReader reader = new EndianBinaryReader(MiscUtil.Conversion.EndianBitConverter.Little, instream);
            instream.Seek(-1, SeekOrigin.End);
            while (instream.ReadByte() == 0)
            {
                zeros++;
                instream.Seek(-2, SeekOrigin.Current);
            }
            instream.Seek(8, SeekOrigin.Begin);
            int size = reader.ReadInt32();
            instream.Seek(4, SeekOrigin.Current);
            //instream.Seek(16, SeekOrigin.Begin);
            ZOutputStream zlibstream = new ZOutputStream(memorystream);

            byte[] buffer = new byte[size];
            instream.Read(buffer, 0, buffer.Length);
            zlibstream.Write(buffer, 0, buffer.Length);

                instream.Close();
        }

        public MemoryStream StreamtoMemory(Stream instream, bool xboxfile)
        {
            int size;

            EndianBinaryReader reader = new EndianBinaryReader(MiscUtil.Conversion.EndianBitConverter.Little, instream);
            if (xboxfile)
                reader = new EndianBinaryReader(MiscUtil.Conversion.EndianBitConverter.Big, instream);
            instream.Seek(8, SeekOrigin.Current);
            size = reader.ReadInt32();
            instream.Seek(4, SeekOrigin.Current);

            byte[] buffer = new byte[size];
            instream.Read(buffer, 0, buffer.Length);

            return memorydecompress(buffer);
        }

        public void compressFile(Stream inFileStream, string outFile, bool xboxfile)
        {
            FileStream outFileStream = new FileStream(outFile, System.IO.FileMode.Create);
            inFileStream.Seek(0, SeekOrigin.Begin);
            byte[] buffer = { 0, 1, 1, 87, 69, 83, 89, 83 };
            outFileStream.Write(buffer, 0, 8);
            outFileStream.Seek(16, SeekOrigin.Begin);
            ZOutputStream outZStream = new ZOutputStream(outFileStream, 7);
            try
            {
                CopyStream(inFileStream, outZStream);
            }
            finally
            {
                outZStream.Close();
                outFileStream.Close();

                FileStream last = new FileStream(outFile, FileMode.Open);

                EndianBinaryWriter bwin = new MiscUtil.IO.EndianBinaryWriter(MiscUtil.Conversion.EndianBitConverter.Little, last);
                if (xboxfile)
                    bwin = new MiscUtil.IO.EndianBinaryWriter(MiscUtil.Conversion.EndianBitConverter.Big, last);

                last.Seek(8, SeekOrigin.Begin);
                bwin.Write((int)last.Length - 16);
                bwin.Write((int)inFileStream.Length);

                bwin.Close();
                last.Close();
            }
        }

        public MemoryStream compresstoStream(Stream inFileStream, bool xboxfile)
        {
            MemoryStream outFileStream = new MemoryStream();
            inFileStream.Seek(0, SeekOrigin.Begin);
            byte[] buffer = { 0, 1, 1, 87, 69, 83, 89, 83 };
            outFileStream.Write(buffer, 0, 8);
            outFileStream.Seek(16, SeekOrigin.Begin);
            ZOutputStream outZStream = new ZOutputStream(outFileStream, 7);
            try
            {
                CopyStream(inFileStream, outZStream);
            }
            finally
            {
                outZStream.finish();

                EndianBinaryWriter bwin = new MiscUtil.IO.EndianBinaryWriter(MiscUtil.Conversion.EndianBitConverter.Little, outFileStream);
                if (xboxfile)
                    bwin = new MiscUtil.IO.EndianBinaryWriter(MiscUtil.Conversion.EndianBitConverter.Big, outFileStream);

                outFileStream.Seek(8, SeekOrigin.Begin);
                bwin.Write((int)outFileStream.Length - 16);
                bwin.Write((int)inFileStream.Length);
            }

            outFileStream.Seek(0, SeekOrigin.Begin);

            return outFileStream;
        }

        public MemoryStream compresstoStreamNoHeader(Stream inFileStream)
        {
            MemoryStream outFileStream = new MemoryStream();
            inFileStream.Seek(0, SeekOrigin.Begin);
            ZOutputStream outZStream = new ZOutputStream(outFileStream, 7);
            try
            {
                CopyStream(inFileStream, outZStream);
            }
            finally
            {
                outZStream.finish();
            }

            outFileStream.Seek(0, SeekOrigin.Begin);

            return outFileStream;
        }

        public MemoryStream[] splitmultifile(Stream input, bool xboxfile)
        {
            int partcount, fcursor, fhead, psize, noffset;
            fcursor = 0;
            psize = 0;
            byte[] buffer;
            MemoryStream[] subfiles;

            hasnames = false;

            input.Seek(0, SeekOrigin.Begin);
            MiscUtil.Conversion.EndianBitConverter endian;
            endian = MiscUtil.Conversion.EndianBitConverter.Little;
            if (xboxfile)
                endian = MiscUtil.Conversion.EndianBitConverter.Big;
            EndianBinaryReader inread = new EndianBinaryReader(endian, input);

            partcount = inread.ReadInt32();
            inread.ReadInt32();

            fhead = (int)input.Position;

            subfiles = new MemoryStream[partcount];

            for (int i = 0; i < partcount; i++)
            {
                fcursor = inread.ReadInt32();
                psize = inread.ReadInt32();
                buffer = new byte[psize];
                noffset = inread.ReadInt32();
                fhead += 12;

                input.Seek(fcursor, SeekOrigin.Begin);
                input.Read(buffer, 0, psize);

                subfiles[i] = new MemoryStream();
                subfiles[i].Write(buffer, 0, psize);

                if (noffset > 0)
                {
                    if (hasnames)
                    {
                        input.Seek(noffset, SeekOrigin.Begin);
                        fnames[i] = loadstring(input);
                    }
                    else
                    {
                        fnames = new string[partcount];
                        hasnames = true;

                        input.Seek(noffset, SeekOrigin.Begin);
                        fnames[i] = loadstring(input);
                    }
                }

                subfiles[i].Seek(0, SeekOrigin.Begin);
                input.Seek(fhead, SeekOrigin.Begin);
            }

            return subfiles;
        }



        public MemoryStream[] splitmultizlibfile(Stream input)
        {
            int partcount, fcursor, fhead, psize;
            fcursor = 0;
            psize = 0;
            byte[] buffer;
            MemoryStream[] subfiles;

            input.Seek(16, SeekOrigin.Begin);
            BinaryReader inread = new BinaryReader(input);
            long flag;

            partcount = inread.ReadInt32();
            inread.ReadInt32();

            fhead = (int)input.Position;

            subfiles = new MemoryStream[partcount];

            for (int i = 0; i < partcount; i++)
            {
                fcursor = inread.ReadInt32() + 16;
                psize = inread.ReadInt32();
                inread.ReadInt32();
                fhead += 12;

                input.Seek(fcursor, SeekOrigin.Begin);
                flag = inread.ReadInt64();
                if (flag == 0x5359534557010000 || flag == 0x5359534557010100 || flag == 0x5359534557000100)
                {
                    psize = inread.ReadInt32();
                    if (inread.ReadInt32() > 0)
                    {
                        buffer = new byte[psize];
                        input.Read(buffer, 0, psize);

                        subfiles[i] = new MemoryStream();
                        subfiles[i] = memorydecompress(buffer);
                    }
                    else
                    {
                        buffer = new byte[psize];
                        input.Read(buffer, 0, psize);

                        subfiles[i] = new MemoryStream(buffer);
                    }
                }
                else
                {
                    input.Seek(-8, SeekOrigin.Current);
                    buffer = new byte[psize];
                    input.Read(buffer, 0, psize);
                    subfiles[i] = new MemoryStream(buffer);
                }

                subfiles[i].Seek(0, SeekOrigin.Begin);
                input.Seek(fhead, SeekOrigin.Begin);
            }

            return subfiles;
        }

        public MemoryStream[] splitmultizlibfilefromafs(Stream input)
        {
            int goffset, partcount, fcursor, fhead, psize;
            fcursor = 0;
            psize = 0;
            byte[] buffer;
            MemoryStream[] subfiles;

            goffset = (int)input.Position;
            input.Seek(goffset + 16, SeekOrigin.Begin);
            BinaryReader inread = new BinaryReader(input);

            partcount = inread.ReadInt32();
            inread.ReadInt32();

            fhead = (int)input.Position;

            subfiles = new MemoryStream[partcount];

            for (int i = 0; i < partcount; i++)
            {
                fcursor = inread.ReadInt32() + 16;
                inread.ReadInt32();
                inread.ReadInt32();
                fhead += 12;

                input.Seek(goffset + fcursor, SeekOrigin.Begin);
                inread.ReadInt32();
                inread.ReadInt32();
                psize = inread.ReadInt32();
                inread.ReadInt32();
                buffer = new byte[psize];
                input.Read(buffer, 0, psize);

                subfiles[i] = new MemoryStream();
                subfiles[i] = memorydecompress(buffer);

                input.Seek(goffset + fhead, SeekOrigin.Begin);
            }

            return subfiles;
        }

        public void savemultizlib(MemoryStream[] subfiles, string filename)
        {
            throw new Exception("STELIOS: Not implemented yet!");
            int currentpos, headerpos;
            MemoryStream compressed;

            FileStream fstream = new FileStream(filename, FileMode.OpenOrCreate);
            BinaryWriter writer = new BinaryWriter(fstream);

            fstream.Write(new byte[] { 0x00, 0x01, 0x00, 0x57, 0x45, 0x53, 0x59, 0x53, 0x54, 0x21, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, 16);
            writer.Write((int)subfiles.Length);
            writer.Write((int)8);

            headerpos = (int)fstream.Position;

            for (int i = 0; i < subfiles.Length; i++)
            {
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
            }

            for (int i = 0; i < subfiles.Length; i++)
            {
                currentpos = (int)fstream.Position;
                compressed = compresstoStreamNoHeader(subfiles[i]);
                compressed.WriteTo(fstream);
                fstream.Seek(headerpos, SeekOrigin.Begin);
                writer.Write((int)currentpos);
                writer.Write((int)compressed.Length);
                writer.Write((uint)0xffffffff);
            }

            writer.Close();
            fstream.Close();
        }

        public void savemultiWESYS(MemoryStream[] subfiles, string filename)
        {
            int currentpos, headerpos;

            FileStream fstream = new FileStream(filename, FileMode.OpenOrCreate);
            BinaryWriter writer = new BinaryWriter(fstream);

            fstream.Write(new byte[] { 0x00, 0x01, 0x00, 0x57, 0x45, 0x53, 0x59, 0x53, 0x54, 0x21, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, 16);
            writer.Write((int)subfiles.Length);
            writer.Write((int)8);

            headerpos = (int)fstream.Position;

            for (int i = 0; i < subfiles.Length; i++)
            {
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
            }

            for (int i = 0; i < subfiles.Length; i++)
            {
                currentpos = (int)fstream.Position;
                subfiles[i].Seek(0, SeekOrigin.Begin);
                subfiles[i].WriteTo(fstream);
                fstream.Seek(headerpos, SeekOrigin.Begin);
                writer.Write((int)(currentpos-0x10));
                writer.Write((int)subfiles[i].Length);
                writer.Write((uint)0xfffffff0);
                headerpos += 0xC;
                fstream.Seek(0, SeekOrigin.End);
            }

            writer.Close();
            fstream.Close();
        }

        public MemoryStream mergemultifile(MemoryStream[] subparts, bool xboxfile)
        {
            int fhead, fcursor, partfiles;
            byte[] buffer;

            partfiles = subparts.Length;

            MemoryStream output = new MemoryStream();
            MiscUtil.Conversion.EndianBitConverter endian;
            endian = MiscUtil.Conversion.EndianBitConverter.Little;
            if (xboxfile)
                endian = MiscUtil.Conversion.EndianBitConverter.Big;
            EndianBinaryWriter outread = new EndianBinaryWriter(endian, output);

            outread.Write(partfiles);
            outread.Write(8);
            fhead = (int)output.Position;

            for (int i = 0; i < partfiles; i++)
            {
                outread.Write(0);
                outread.Write(0);
                outread.Write(-1);
            }

            for (int i = 0; i < partfiles; i++)
            {
                fcursor = (int)output.Position;
                output.Seek(fhead, SeekOrigin.Begin);
                outread.Write((int)fcursor);
                outread.Write((int)subparts[i].Length);
                fhead += 12;
                output.Seek(fcursor, SeekOrigin.Begin);

                buffer = new byte[subparts[i].Length];
                subparts[i].Seek(0, SeekOrigin.Begin);
                subparts[i].Read(buffer, 0, buffer.Length);
                output.Write(buffer, 0, buffer.Length);
            }

            if (hasnames)
            {
                output.Seek(16, SeekOrigin.Begin);
                fhead = 16;

                for (int i = 0; i < partfiles; i++)
                {
                    outread.Write((int)output.Length);
                    output.Seek(output.Length, SeekOrigin.Begin);
                    outread.Write(fnames[i].ToCharArray());
                    outread.Write((short)0);

                    fhead += 12;
                    output.Seek(fhead, SeekOrigin.Begin);
                }
            }

            output.Seek(0, SeekOrigin.Begin);
            return output;
        }

        public void makesef(Stream[] files, string seffilename, bool trianglestrip)
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.NumberDecimalSeparator = ".";

            //Create SEF file            
            FileStream seffile = new FileStream(seffilename, FileMode.Create, FileAccess.Write);
            StreamWriter sefstream = new StreamWriter(seffile);

            sefstream.Write("//Stadium Exchange File (c)2007 warpjavier\n");
            sefstream.Write("\nWeather = \"DF\" \n\n");
            sefstream.Write("Materials = 0\n\n");
            sefstream.Write("\nLights = 0\n\n");
            sefstream.Write("\nMeshes = {0}\n\n", files.Length);

            SeekOrigin SEEK_SET = SeekOrigin.Begin;
            SeekOrigin SEEK_CUR = SeekOrigin.Current;
            //SEF main information
            for (int t = 0; t < files.Length; t++)
            {
                BinaryReader inread = new BinaryReader(files[t]);

                files[t].Seek(40, SeekOrigin.Begin);
                long sections;
                sections = inread.ReadUInt32();
                sefstream.Write("Name = \"MESH_{0:000}\" {1}\n", t, sections);

                files[t].Seek(52, SeekOrigin.Begin);
                long pointer;
                pointer = inread.ReadUInt32();

                //lines for materials-textures
                files[t].Seek(44, SeekOrigin.Begin);
                long mat_offset;
                mat_offset = inread.ReadUInt32();
                //CHANGED TO 48 (FROM 52) FOR TEST
                mat_offset += 52;

                files[t].Seek(80, SEEK_SET);
                long mat_cant;
                mat_cant = inread.ReadUInt32();

                files[t].Seek(128, SEEK_SET);
                long tex_offset;
                tex_offset = inread.ReadUInt32();
                tex_offset += 8;

                files[t].Seek(mat_offset, SEEK_SET);
                ushort[] section_mat = new ushort[sections];
                for (int i = 0; i < sections; i++)
                {
                    section_mat[i] = inread.ReadUInt16();
                    files[t].Seek(94, SEEK_CUR);
                }

                files[t].Seek(tex_offset, SEEK_SET);
                byte[] textur1 = new byte[mat_cant];
                byte[] textur2 = new byte[mat_cant];
                string[] textur = new string[mat_cant];
                for (int c = 0; c < mat_cant; c++)
                {
                    textur1[c] = inread.ReadByte();
                    textur2[c] = inread.ReadByte();
                    textur[c] = String.Format("{0}{1}", textur2[c].ToString("X").PadLeft(2, '0'), textur1[c].ToString("X").PadLeft(2, '0'));
                    files[t].Seek(14, SEEK_CUR);
                }

                files[t].Seek(pointer, SEEK_SET);

                //carga offsets
                _offsets[] offsets = new _offsets[sections];
                int parts, cantidad, parametro = 0;
                byte ratio, newratio;
                _xyz[] row;
                for (int x = 0; x < sections; x++)
                {
                    offsets[x].offset_vert = inread.ReadUInt32();
                    offsets[x].cant_vert = inread.ReadUInt32();
                    offsets[x].nose1 = inread.ReadByte();
                    offsets[x].magic = inread.ReadByte();
                    offsets[x].nose2 = (short)inread.ReadUInt16();
                    offsets[x].nose3 = inread.ReadUInt32();
                    offsets[x].nose4 = inread.ReadUInt32();
                    offsets[x].nose5 = inread.ReadUInt32();
                    offsets[x].nose6 = inread.ReadUInt32();
                    offsets[x].nose7 = inread.ReadUInt32();
                    offsets[x].offset_ind = inread.ReadUInt32();
                    offsets[x].cant_ind = inread.ReadUInt32();
                    files[t].Seek(24, SEEK_CUR);
                    offsets[x].offset_vert += (ushort)(pointer + (64 * x));
                    offsets[x].offset_ind += (ushort)(pointer + (64 * x) + 32);
                }
                for (int a = 0; a < sections; a++)
                {
                    files[t].Seek(offsets[a].offset_vert, SEEK_SET);
                    parts = (int)offsets[a].cant_vert;
                    ratio = offsets[a].magic;
                    sefstream.Write("{0:000}-00 {1}\n", parametro++, textur[section_mat[a]]);
                    sefstream.Write("{0}\n", parts);

                    row = new _xyz[parts];
                    for (int l = 0; l < parts; l++)
                    {
                        row[l].x = inread.ReadSingle();
                        row[l].z = inread.ReadSingle();
                        row[l].y = inread.ReadSingle();
                        if (ratio == 40)
                        {
                            //input.Seek(20, SEEK_CUR);
                            //row[l].tu = inread.ReadSingle();
                            //row[l].tv = inread.ReadSingle();
                            files[t].Seek(20, SEEK_CUR);
                            row[l].tu = inread.ReadSingle();
                            row[l].tv = inread.ReadSingle();
                            files[t].Seek(0, SEEK_CUR);
                        }
                        else if (ratio == 36)
                        {
                            files[t].Seek(12, SEEK_CUR);
                            row[l].tu = inread.ReadSingle();
                            row[l].tv = inread.ReadSingle();
                            files[t].Seek(4, SEEK_CUR);
                        }
                        else if (ratio == 44)
                        {
                            files[t].Seek(20, SEEK_CUR);
                            row[l].tu = inread.ReadSingle();
                            row[l].tv = inread.ReadSingle();
                            files[t].Seek(4, SEEK_CUR);
                        }
                        else if (ratio == 48)
                        {
                            files[t].Seek(12, SEEK_CUR);
                            row[l].tu = inread.ReadSingle();
                            row[l].tv = inread.ReadSingle();
                            files[t].Seek(16, SEEK_CUR);
                        }
                        else if (ratio == 32)
                        {
                            files[t].Seek(12, SEEK_CUR);
                            row[l].tu = inread.ReadSingle();
                            row[l].tv = inread.ReadSingle();
                        }
                        else
                        {
                            newratio = (byte)(ratio - 40);
                            files[t].Seek((20 + newratio), SEEK_CUR);
                            row[l].tu = inread.ReadSingle();
                            row[l].tv = inread.ReadSingle();
                        }
                    }

                    for (int m = 0; m < parts; m++)
                    {
                        sefstream.Write(String.Format(nfi, "{0:0.000000} ", row[m].x / 2));
                        sefstream.Write(String.Format(nfi, "{0:0.000000} ", row[m].y / 2));
                        sefstream.Write(String.Format(nfi, "{0:0.000000} ", row[m].z / 2));
                        sefstream.Write(String.Format(nfi, "{0:0.000000} ", row[m].tu));
                        sefstream.Write(String.Format(nfi, "{0:0.000000} ", row[m].tv));
                        sefstream.Write("0xFFFFFFFF\n");
                    }

                    files[t].Seek(offsets[a].offset_ind, SEEK_SET);
                    cantidad = (int)offsets[a].cant_ind;

                    int triangles = 0;
                    int u = 0;
                    int idx_acum = 0;
                    short[] indices = new short[cantidad * 2];
                    bool si = true;
                    triang[] tri = new triang[cantidad * 2];
                    for (int s = 0; s < cantidad; s++)
                    {
                        indices[s] = (short)inread.ReadUInt16();
                    }
                    if (trianglestrip)
                    {
                        for (u = idx_acum; u < cantidad - 2; u++)
                        {
                            if ((indices[u] != indices[u + 1]) && (indices[u + 1] != indices[u + 2]) && (indices[u + 2] != indices[u]))
                            {
                                if (si)
                                {
                                    tri[triangles].z = indices[u + 0];
                                    tri[triangles].y = indices[u + 1];
                                    tri[triangles].x = indices[u + 2];
                                    si = false;
                                }
                                else
                                {
                                    tri[triangles].x = indices[u + 0];
                                    tri[triangles].y = indices[u + 1];
                                    tri[triangles].z = indices[u + 2];
                                    si = true;
                                }
                                triangles++;
                            }
                            else
                            {
                                if (si)
                                    si = false;
                                else
                                    si = true;
                            }
                        }
                    }
                    else
                    {
                        for (u = idx_acum; u < cantidad - 2; u += 3)
                        {
                            if ((indices[u] != indices[u + 1]) && (indices[u + 1] != indices[u + 2]) && (indices[u + 2] != indices[u]))
                            {
                                tri[triangles].z = indices[u + 0];
                                tri[triangles].y = indices[u + 1];
                                tri[triangles].x = indices[u + 2];
                                triangles++;
                            }
                            else
                            {
                                if (si)
                                    si = false;
                                else
                                    si = true;
                            }
                        }
                    }
                    sefstream.Write("{0}\n", triangles);
                    for (int g = 0; g < triangles; g++)
                    {
                        sefstream.Write("{0} {1} {2}\n", ((tri[g].x)), ((tri[g].y)), ((tri[g].z)));
                    }
                }
            }

            sefstream.Close();
            seffile.Close();
        }

        public _stadium readKTMDL(Stream[] files, bool trianglestrip)
        {
            _stadium model = new _stadium();

            SeekOrigin SEEK_SET = SeekOrigin.Begin;
            SeekOrigin SEEK_CUR = SeekOrigin.Current;

            model.meshes = new _mesh[files.Length];
            for (int t = 0; t < files.Length; t++)
            {
                model.meshes[t] = new _mesh();
                BinaryReader inread = new BinaryReader(files[t]);

                files[t].Seek(40, SeekOrigin.Begin);
                uint sections;
                sections = inread.ReadUInt32();
                model.meshes[t].objcount = (int)sections;
                model.meshes[t].objs = new _object[sections];

                files[t].Seek(52, SeekOrigin.Begin);
                long pointer;
                pointer = inread.ReadUInt32();

                //lines for materials-textures
                files[t].Seek(44, SeekOrigin.Begin);
                long mat_offset;
                mat_offset = inread.ReadUInt32();
                //CHANGED TO 48 (FROM 52) FOR TEST
                mat_offset += 52;

                files[t].Seek(80, SEEK_SET);
                long mat_cant;
                mat_cant = inread.ReadUInt32();

                files[t].Seek(128, SEEK_SET);
                long tex_offset;
                tex_offset = inread.ReadUInt32();
                tex_offset += 8;

                files[t].Seek(mat_offset, SEEK_SET);
                ushort[] section_mat = new ushort[sections];
                for (int i = 0; i < sections; i++)
                {
                    section_mat[i] = inread.ReadUInt16();
                    files[t].Seek(94, SEEK_CUR);
                }

                files[t].Seek(tex_offset, SEEK_SET);
                short[] textur1 = new short[mat_cant];
                string[] textur = new string[mat_cant];
                for (int c = 0; c < mat_cant; c++)
                {
                    textur1[c] = inread.ReadInt16();
                    textur[c] = textur1[c].ToString("x");
                    files[t].Seek(14, SEEK_CUR);
                }

                files[t].Seek(pointer, SEEK_SET);

                //carga offsets
                _offsets[] offsets = new _offsets[sections];
                int parts, cantidad;
                byte ratio, newratio;
                for (int x = 0; x < sections; x++)
                {
                    offsets[x].offset_vert = inread.ReadUInt32();
                    offsets[x].cant_vert = inread.ReadUInt32();
                    offsets[x].nose1 = inread.ReadByte();
                    offsets[x].magic = inread.ReadByte();
                    offsets[x].nose2 = (short)inread.ReadUInt16();
                    offsets[x].nose3 = inread.ReadUInt32();
                    offsets[x].nose4 = inread.ReadUInt32();
                    offsets[x].nose5 = inread.ReadUInt32();
                    offsets[x].nose6 = inread.ReadUInt32();
                    offsets[x].nose7 = inread.ReadUInt32();
                    offsets[x].offset_ind = inread.ReadUInt32();
                    offsets[x].cant_ind = inread.ReadUInt32();
                    files[t].Seek(24, SEEK_CUR);
                    offsets[x].offset_vert += (ushort)(pointer + (64 * x));
                    offsets[x].offset_ind += (ushort)(pointer + (64 * x) + 32);
                }
                for (int a = 0; a < sections; a++)
                {
                    files[t].Seek(offsets[a].offset_vert, SEEK_SET);
                    parts = (int)offsets[a].cant_vert;
                    ratio = offsets[a].magic;
                    if (textur.Length > 0)
                        model.meshes[t].objs[a].textid = textur[section_mat[a]];
                    else
                        model.meshes[t].objs[a].textid = "0000";
                    model.meshes[t].objs[a].vertcount = parts;
                    model.meshes[t].objs[a].verts = new _vertex[parts];

                    for (int l = 0; l < parts; l++)
                    {
                        model.meshes[t].objs[a].verts[l].x = inread.ReadSingle();
                        model.meshes[t].objs[a].verts[l].z = inread.ReadSingle();
                        model.meshes[t].objs[a].verts[l].y = inread.ReadSingle();
                        if (ratio == 40)
                        {
                            //input.Seek(20, SEEK_CUR);
                            //row[l].tu = inread.ReadSingle();
                            //row[l].tv = inread.ReadSingle();
                            files[t].Seek(20, SEEK_CUR);
                            model.meshes[t].objs[a].verts[l].u = inread.ReadSingle();
                            model.meshes[t].objs[a].verts[l].v = inread.ReadSingle();
                            files[t].Seek(0, SEEK_CUR);
                        }
                        else if (ratio == 36)
                        {
                            files[t].Seek(12, SEEK_CUR);
                            model.meshes[t].objs[a].verts[l].u = inread.ReadSingle();
                            model.meshes[t].objs[a].verts[l].v = inread.ReadSingle();
                            files[t].Seek(4, SEEK_CUR);
                        }
                        else if (ratio == 44)
                        {
                            files[t].Seek(20, SEEK_CUR);
                            model.meshes[t].objs[a].verts[l].u = inread.ReadSingle();
                            model.meshes[t].objs[a].verts[l].v = inread.ReadSingle();
                            files[t].Seek(4, SEEK_CUR);
                        }
                        else if (ratio == 48)
                        {
                            files[t].Seek(12, SEEK_CUR);
                            model.meshes[t].objs[a].verts[l].u = inread.ReadSingle();
                            model.meshes[t].objs[a].verts[l].v = inread.ReadSingle();
                            files[t].Seek(16, SEEK_CUR);
                        }
                        else if (ratio == 32)
                        {
                            files[t].Seek(12, SEEK_CUR);
                            model.meshes[t].objs[a].verts[l].u = inread.ReadSingle();
                            model.meshes[t].objs[a].verts[l].v = inread.ReadSingle();
                        }
                        else if (ratio == 24)
                        {
                            model.meshes[t].objs[a].verts[l].u = inread.ReadSingle();
                            model.meshes[t].objs[a].verts[l].v = inread.ReadSingle();
                            files[t].Seek(4, SEEK_CUR);
                        }
                        else
                        {
                            newratio = (byte)(ratio - 40);
                            files[t].Seek((20 + newratio), SEEK_CUR);
                            model.meshes[t].objs[a].verts[l].u = inread.ReadSingle();
                            model.meshes[t].objs[a].verts[l].v = inread.ReadSingle();
                        }
                    }

                    files[t].Seek(offsets[a].offset_ind, SEEK_SET);
                    cantidad = (int)offsets[a].cant_ind;

                    int triangles = 0;
                    int u = 0;
                    short[] indices = new short[cantidad * 2];
                    bool si = true;
                    triang[] tri = new triang[cantidad * 2];
                    for (int s = 0; s < cantidad; s++)
                    {
                        indices[s] = (short)inread.ReadUInt16();
                    }
                    if (trianglestrip)
                    {
                        model.meshes[t].objs[a].indcount = cantidad - 2;
                        model.meshes[t].objs[a].indices = new _triangle[cantidad - 2];
                        for (u = 0; u < cantidad - 2; u++)
                        {
                            if ((indices[u] != indices[u + 1]) && (indices[u + 1] != indices[u + 2]) && (indices[u + 2] != indices[u]))
                            {
                                if (si)
                                {
                                    model.meshes[t].objs[a].indices[triangles].z = indices[u + 0];
                                    model.meshes[t].objs[a].indices[triangles].y = indices[u + 1];
                                    model.meshes[t].objs[a].indices[triangles].x = indices[u + 2];
                                    si = false;
                                }
                                else
                                {
                                    model.meshes[t].objs[a].indices[triangles].x = indices[u + 0];
                                    model.meshes[t].objs[a].indices[triangles].y = indices[u + 1];
                                    model.meshes[t].objs[a].indices[triangles].z = indices[u + 2];
                                    si = true;
                                }
                                triangles++;
                            }
                            else
                            {
                                if (si)
                                    si = false;
                                else
                                    si = true;
                            }
                        }
                    }
                    else
                    {
                        model.meshes[t].objs[a].indcount = cantidad / 3;
                        model.meshes[t].objs[a].indices = new _triangle[cantidad / 3];
                        for (u = 0; u < cantidad - 2; u += 3)
                        {
                            if ((indices[u] != indices[u + 1]) && (indices[u + 1] != indices[u + 2]) && (indices[u + 2] != indices[u]))
                            {
                                model.meshes[t].objs[a].indices[triangles].z = indices[u + 0];
                                model.meshes[t].objs[a].indices[triangles].y = indices[u + 1];
                                model.meshes[t].objs[a].indices[triangles].x = indices[u + 2];
                                triangles++;
                            }
                            else
                            {
                                if (si)
                                    si = false;
                                else
                                    si = true;
                            }
                        }
                    }
                    model.meshes[t].objs[a].indcount = triangles;
                    Array.Resize(ref model.meshes[t].objs[a].indices, triangles);
                }
            }

            return model;
        }

        public _stadium readSef(string filename)
        {
            _stadium tempstadium = new _stadium();

            string line;
            tempstadium.crowd.sides = new _side[20];
            int meshescount, textscount, crowdside;

            //Select a SEF to make part
            StreamReader input = new StreamReader(filename, Encoding.Default);

            if (input.ReadLine() != "//Stadium Exchange File (c)2007 warpjavier")
                throw new Exception("This file does not seem to be a stadium.");

            if (input.ReadLine() != "//2008 format by liberostelios")
                throw new Exception("This is not a SEF 2008 stadium file.");

            do
            {
                line = input.ReadLine();
            } while (!line.StartsWith("Materials"));

            textscount = Convert.ToInt32(line.Split(' ')[2]);
            tempstadium.textures = new _texture[textscount];

            do
            {
                line = input.ReadLine();
            } while (line.Split(' ').Length < 2);

            for (int i = 0; i < textscount; i++)
            {
                tempstadium.textures[i].id = (short)int.Parse(line.Split(' ')[0], System.Globalization.NumberStyles.HexNumber, null);
                tempstadium.textures[i].file = line.Split('"')[1];
                line = input.ReadLine();
            }

            do
            {
                line = input.ReadLine();
            } while (!line.StartsWith("Meshes"));

            string[] pao = { " = " };
            meshescount = Convert.ToInt32(line.Split(pao, StringSplitOptions.RemoveEmptyEntries)[1]);
            tempstadium.meshes = new _mesh[meshescount];
            for (int i = 0; i < meshescount; i++)
            {
                do
                {
                    line = input.ReadLine();
                } while (!line.StartsWith("Name"));
                tempstadium.meshes[i].name = line.Split('"')[1];
                tempstadium.meshes[i].twosides = false;
                if (tempstadium.meshes[i].name == "ROOF")
                    tempstadium.meshes[i].twosides = true;
                if (tempstadium.meshes[i].name.Split('_').Length == 2)
                    if (tempstadium.meshes[i].name.Split('_')[1] == "SIDE")
                        tempstadium.meshes[i].twosides = true;
                tempstadium.meshes[i].objcount = int.Parse(line.Split(' ')[3]);
                tempstadium.meshes[i].objs = new _object[tempstadium.meshes[i].objcount];
                for (int j = 0; j < tempstadium.meshes[i].objcount; j++)
                {
                    line = input.ReadLine();
                    tempstadium.meshes[i].objs[j].textid = line.Split(' ')[1];
                    line = input.ReadLine();
                    tempstadium.meshes[i].objs[j].vertcount = int.Parse(line);
                    tempstadium.meshes[i].objs[j].verts = new _vertex[tempstadium.meshes[i].objs[j].vertcount];

                    for (int k = 0; k < tempstadium.meshes[i].objs[j].vertcount; k++)
                    {
                        line = input.ReadLine();
                        tempstadium.meshes[i].objs[j].verts[k].add(line.Split(' '));
                    }
                    line = input.ReadLine();
                    tempstadium.meshes[i].objs[j].indcount = int.Parse(line);
                    tempstadium.meshes[i].objs[j].indices = new _triangle[tempstadium.meshes[i].objs[j].indcount];

                    for (int k = 0; k < tempstadium.meshes[i].objs[j].indcount; k++)
                    {
                        line = input.ReadLine();
                        tempstadium.meshes[i].objs[j].indices[k].add(line.Split(' '));
                    }
                }
            }

            do
            {
                line = input.ReadLine();
                if (line == null)
                {
                    break;
                }
            } while (!line.StartsWith("Crowd"));

            if (line != null)
            {
                tempstadium.crowd.hascrowd = true;
                line = input.ReadLine();
                while ((line.Split(pao, StringSplitOptions.RemoveEmptyEntries).Length == 2))
                {
                    crowdside = crowdid(line.Split('"')[1]);
                    tempstadium.crowd.sides[crowdside].quads = new _quad[Convert.ToInt32(line.Split(pao, StringSplitOptions.RemoveEmptyEntries)[1])];

                    for (int i = 0; i < tempstadium.crowd.sides[crowdside].quads.Length; i++)
                    {
                        tempstadium.crowd.sides[crowdside].quads[i].verts = new _simplevert[4];
                        for (int j = 0; j < 4; j++)
                        {
                            line = input.ReadLine();

                            tempstadium.crowd.sides[crowdside].quads[i].verts[j].add(line.Split(' '));
                        }
                    }

                    tempstadium.crowd.sides[crowdside].calculateparameters();

                    line = input.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                }
            }
            else
            {
                tempstadium.crowd.hascrowd = false;
            }

            input.Close();

            return tempstadium;
        }

        public Stream makepartfile(_mesh mymesh, bool backside)
        {
            byte[] buffer;
            int section, filesize, i, j, subpartsoff, fcursor, fstveroff;
            mymesh.calculatebounds();
            MemoryStream fstr = new MemoryStream();
            BinaryWriter binw = new BinaryWriter(fstr);

            fstr.Write(PESLibrary.Properties.Resources.headtemplate, 0, (int)PESLibrary.Properties.Resources.headtemplate.Length);
            binw.Write(mymesh.objcount);
            binw.Write(448);
            binw.Write(mymesh.objcount * 2);
            section = 448 + mymesh.objcount * 96;
            binw.Write(section);
            binw.Write(0);
            binw.Write(448);
            binw.Write(2);
            section += mymesh.objcount * 64 + 16;
            binw.Write(section);
            binw.Write(mymesh.objcount);
            section += 144;
            binw.Write(section);
            binw.Write(mymesh.objcount);
            section += mymesh.objcount * 176;
            binw.Write(section);
            binw.Write(1);
            binw.Write(section - mymesh.objcount * 176 - 48);
            section += 80 * mymesh.objcount;
            binw.Write(section);
            binw.Write(192);
            binw.Write(0);
            section = 448 + mymesh.objcount * 96;
            binw.Write(section);
            section += mymesh.objcount * 64;
            binw.Write(section);
            binw.Write(0);//Change later - 1st vertex data opposite offset (from end of file)
            binw.Write(0);//Change later - 1st vertex data offset
            binw.Write(mymesh.objcount);
            section += (mymesh.objcount + 1) * 160;
            binw.Write(section);
            binw.Write(0);
            binw.Write(3);
            section -= (mymesh.objcount + 1) * 160;
            binw.Write(section);
            binw.Write(0);//Change later - File size

            fstr.Write(PESLibrary.Properties.Resources.temp1, 0, (int)PESLibrary.Properties.Resources.temp1.Length);

            buffer = new byte[4];
            getsideprops(mymesh.name, buffer);

            binw.Seek(208, SeekOrigin.Begin);
            binw.Write(buffer[0]);
            binw.Seek(240, SeekOrigin.Begin);
            binw.Write(buffer, 1, 3);
            binw.Seek(448, SeekOrigin.Begin);

            //Materials Section
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(66240);
                binw.Write(i);
                binw.Write(0);
                binw.Write(0);
                binw.Write(1);
                binw.Write((mymesh.objcount * 96) - (i * 32));
                binw.Write(1);
                binw.Write((6 * i + 1) * (-16));
                binw.Write(1);
                binw.Write((mymesh.objcount * 96) - ((i - 1) * 32));
                binw.Write(0);
                binw.Write(1);
                binw.Write(i);
                for (j = 0; j < 11; j++)
                {
                    binw.Write(0);
                }
            }

            //Subparts description
            subpartsoff = (int)fstr.Position;
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(0); //Change later - Vertices offset
                binw.Write(mymesh.objs[i].vertcount);
                binw.Write((byte)1);
                binw.Write((byte)32);
                binw.Write((short)259);
                binw.Write((mymesh.objcount - i) * 64);
                for (j = 0; j < 4; j++)
                {
                    binw.Write(0);
                }
                binw.Write(0); //Change later - Indices offset
                binw.Write(mymesh.objs[i].indcount * 3);
                for (j = 0; j < 6; j++)
                {
                    binw.Write(0);
                }
            }

            //Standard section - before bounding box
            fstr.Write(PESLibrary.Properties.Resources.temp2, 0, (int)PESLibrary.Properties.Resources.temp2.Length);

            //Bounding box
            binw.Write(mymesh.max.x * 2);
            binw.Write(mymesh.max.z * 2);
            binw.Write(-(mymesh.max.y * 2));
            binw.Write(0);
            binw.Write(mymesh.min.x * 2);
            binw.Write(mymesh.min.z * 2);
            binw.Write(-(mymesh.min.y * 2));
            binw.Write(0);

            fstr.Write(PESLibrary.Properties.Resources.temp3, 0, (int)PESLibrary.Properties.Resources.temp3.Length);

            binw.Write(mymesh.max.x * 2);
            binw.Write(mymesh.max.z * 2);
            binw.Write(-(mymesh.max.y * 2));
            binw.Write(0);
            binw.Write(mymesh.min.x * 2);
            binw.Write(mymesh.min.z * 2);
            binw.Write(-(mymesh.min.y * 2));
            binw.Write(0);

            //Strange section
            for (i = 0; i < mymesh.objcount; i++)
            {
                fstr.Write(PESLibrary.Properties.Resources.temp4, 0, (int)PESLibrary.Properties.Resources.temp4.Length);
            }

            //Textures section
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(0);
                binw.Write(0);
                binw.Write(int.Parse(mymesh.objs[i].textid, System.Globalization.NumberStyles.HexNumber, null));
                binw.Write(0);
            }

            //Textures properties section
            for (i = 0; i < mymesh.objcount; i++)
            {
                for (j = 0; j < 2; j++)
                {
                    binw.Write(0);
                }
                binw.Write((short)0);
                binw.Write((short)514);
                binw.Write((short)2);
                binw.Write((short)i);
                for (j = 0; j < 16; j++)
                {
                    binw.Write(0);
                }
            }

            //Image names section
            binw.Write(0);
            binw.Write(12);
            binw.Write(12 + (mymesh.objcount * 16));
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write((mymesh.objcount * 4) + i * 12);
            }
            fstr.Seek(-1, SeekOrigin.Current);
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(String.Format("l_img{0:00}.psd", i));
                fstr.Seek(-12, SeekOrigin.Current);
                binw.Write((byte)0);
                fstr.Seek(11, SeekOrigin.Current);
            }
            binw.Write((byte)0);

            //Last part before vertices
            fstr.Write(PESLibrary.Properties.Resources.temp5, 0, (int)PESLibrary.Properties.Resources.temp5.Length);

            //Vertices and indices section
            fstveroff = (int)fstr.Position;
            for (i = 0; i < mymesh.objcount; i++)
            {
                fcursor = (int)fstr.Position;
                fstr.Seek(subpartsoff, SeekOrigin.Begin);
                binw.Write(fcursor - subpartsoff);
                fstr.Seek(fcursor, SeekOrigin.Begin);
                subpartsoff += 32;

                for (j = 0; j < mymesh.objs[i].vertcount; j++)
                {
                    binw.Write(mymesh.objs[i].verts[j].x * 2);
                    binw.Write(mymesh.objs[i].verts[j].z * 2);
                    binw.Write(-(mymesh.objs[i].verts[j].y * 2));
                    binw.Write(mymesh.objs[i].verts[j].nx);
                    binw.Write(mymesh.objs[i].verts[j].nz);
                    binw.Write(mymesh.objs[i].verts[j].ny);
                    binw.Write(mymesh.objs[i].verts[j].u);
                    binw.Write(mymesh.objs[i].verts[j].v);
                    //binw.Write((byte)mymesh.objs[i].verts[j].vpaint.R);
                    //binw.Write((byte)mymesh.objs[i].verts[j].vpaint.G);
                    //binw.Write((byte)mymesh.objs[i].verts[j].vpaint.B);
                    //binw.Write((byte)mymesh.objs[i].verts[j].vpaint.A);
                }

                fcursor = (int)fstr.Position;
                fstr.Seek(subpartsoff, SeekOrigin.Begin);
                binw.Write(fcursor - subpartsoff);
                fstr.Seek(fcursor, SeekOrigin.Begin);
                subpartsoff += 32;

                if (backside)
                {
                    for (j = 0; j < mymesh.objs[i].indcount; j++)
                    {
                        binw.Write((short)mymesh.objs[i].indices[j].z);
                        binw.Write((short)mymesh.objs[i].indices[j].y);
                        binw.Write((short)mymesh.objs[i].indices[j].x);
                    }
                }
                else
                {
                    for (j = 0; j < mymesh.objs[i].indcount; j++)
                    {
                        binw.Write((short)mymesh.objs[i].indices[j].x);
                        binw.Write((short)mymesh.objs[i].indices[j].y);
                        binw.Write((short)mymesh.objs[i].indices[j].z);
                    }
                }
            }

            //Change last values
            filesize = (int)fstr.Position;
            fstr.Seek(116, SeekOrigin.Begin);
            binw.Write(filesize - fstveroff);
            binw.Write(fstveroff);
            fstr.Seek(144, SeekOrigin.Begin);
            binw.Write(filesize);

            return fstr;
        }

        public Stream makepartfilevpaint(_mesh mymesh, bool backside)
        {
            byte[] buffer;
            int section, filesize, i, j, subpartsoff, fcursor, fstveroff;
            mymesh.calculatebounds();
            MemoryStream fstr = new MemoryStream();
            BinaryWriter binw = new BinaryWriter(fstr);

            fstr.Write(PESLibrary.Properties.Resources.headtemplate, 0, (int)PESLibrary.Properties.Resources.headtemplate.Length);
            binw.Write(mymesh.objcount);
            binw.Write(448);
            binw.Write(mymesh.objcount * 2);
            section = 448 + mymesh.objcount * 96;
            binw.Write(section);
            binw.Write(0);
            binw.Write(448);
            binw.Write(2);
            section += mymesh.objcount * 64 + 16;
            binw.Write(section);
            binw.Write(mymesh.objcount);
            section += 144;
            binw.Write(section);
            binw.Write(mymesh.objcount);
            section += mymesh.objcount * 176;
            binw.Write(section);
            binw.Write(1);
            binw.Write(section - mymesh.objcount * 176 - 48);
            section += 80 * mymesh.objcount;
            binw.Write(section);
            binw.Write(192);
            binw.Write(0);
            section = 448 + mymesh.objcount * 96;
            binw.Write(section);
            section += mymesh.objcount * 64;
            binw.Write(section);
            binw.Write(0);//Change later - vertex data size
            binw.Write(0);//Change later - 1st vertex data offset
            binw.Write(mymesh.objcount);
            section += (mymesh.objcount + 1) * 160;
            binw.Write(section);
            binw.Write(0);
            binw.Write(3);
            section -= (mymesh.objcount + 1) * 160;
            binw.Write(section);
            binw.Write(0);//Change later - File size

            fstr.Write(PESLibrary.Properties.Resources.temp1, 0, (int)PESLibrary.Properties.Resources.temp1.Length);

            buffer = new byte[4];
            getsideprops(mymesh.name, buffer);

            binw.Seek(208, SeekOrigin.Begin);
            binw.Write(buffer[0]);
            binw.Seek(240, SeekOrigin.Begin);
            binw.Write(buffer, 1, 3);
            binw.Seek(448, SeekOrigin.Begin);

            //Materials Section
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(66240);
                binw.Write(i);
                binw.Write(0);
                binw.Write(0);
                binw.Write(1);
                binw.Write((mymesh.objcount * 96) - (i * 32));
                binw.Write(1);
                binw.Write((6 * i + 1) * (-16));
                binw.Write(1);
                binw.Write((mymesh.objcount * 96) - ((i - 1) * 32));
                binw.Write(0);
                binw.Write(1);
                binw.Write(i);
                for (j = 0; j < 11; j++)
                {
                    binw.Write(0);
                }
            }

            //Subparts description
            subpartsoff = (int)fstr.Position;
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(0); //Change later - Vertices offset
                binw.Write(mymesh.objs[i].vertcount);
                binw.Write((byte)1);
                binw.Write((byte)36);
                binw.Write((short)260);
                binw.Write((mymesh.objcount - i) * 64);
                for (j = 0; j < 4; j++)
                {
                    binw.Write(0);
                }
                binw.Write(0); //Change later - Indices offset
                binw.Write(mymesh.objs[i].indcount * 3);
                for (j = 0; j < 6; j++)
                {
                    binw.Write(0);
                }
            }

            //Standard section - before bounding box
            fstr.Write(PESLibrary.Properties.Resources.temp2, 0, (int)PESLibrary.Properties.Resources.temp2.Length);

            //Bounding box
            binw.Write(mymesh.max.x * 2);
            binw.Write(mymesh.max.z * 2);
            binw.Write(-(mymesh.max.y * 2));
            binw.Write(0);
            binw.Write(mymesh.min.x * 2);
            binw.Write(mymesh.min.z * 2);
            binw.Write(-(mymesh.min.y * 2));
            binw.Write(0);

            fstr.Write(PESLibrary.Properties.Resources.temp3, 0, (int)PESLibrary.Properties.Resources.temp3.Length);

            binw.Write(mymesh.max.x * 2);
            binw.Write(mymesh.max.z * 2);
            binw.Write(-(mymesh.max.y * 2));
            binw.Write(0);
            binw.Write(mymesh.min.x * 2);
            binw.Write(mymesh.min.z * 2);
            binw.Write(-(mymesh.min.y * 2));
            binw.Write(0);

            //Strange section
            for (i = 0; i < mymesh.objcount; i++)
            {
                fstr.Write(PESLibrary.Properties.Resources.temp4, 0, (int)PESLibrary.Properties.Resources.temp4.Length);
            }

            //Textures section
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(0);
                binw.Write(0);
                binw.Write(int.Parse(mymesh.objs[i].textid, System.Globalization.NumberStyles.HexNumber, null));
                binw.Write(0);
            }

            //Textures properties section
            for (i = 0; i < mymesh.objcount; i++)
            {
                for (j = 0; j < 2; j++)
                {
                    binw.Write(0);
                }
                binw.Write((short)0);
                binw.Write((short)514);
                binw.Write((short)2);
                binw.Write((short)i);
                for (j = 0; j < 16; j++)
                {
                    binw.Write(0);
                }
            }

            //Image names section
            binw.Write(0);
            binw.Write(12);
            binw.Write(12 + (mymesh.objcount * 16));
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write((mymesh.objcount * 4) + i * 12);
            }
            fstr.Seek(-1, SeekOrigin.Current);
            for (i = 0; i < mymesh.objcount; i++)
            {
                binw.Write(String.Format("l_img{0:00}.psd", i));
                fstr.Seek(-12, SeekOrigin.Current);
                binw.Write((byte)0);
                fstr.Seek(11, SeekOrigin.Current);
            }
            binw.Write((byte)0);

            //Last part before vertices
            fstr.Write(PESLibrary.Properties.Resources.temp5, 0, (int)PESLibrary.Properties.Resources.temp5.Length);

            //Vertices and indices section
            fstveroff = (int)fstr.Position;
            for (i = 0; i < mymesh.objcount; i++)
            {
                fcursor = (int)fstr.Position;
                fstr.Seek(subpartsoff, SeekOrigin.Begin);
                binw.Write(fcursor - subpartsoff);
                fstr.Seek(fcursor, SeekOrigin.Begin);
                subpartsoff += 32;

                for (j = 0; j < mymesh.objs[i].vertcount; j++)
                {
                    binw.Write(mymesh.objs[i].verts[j].x * 2);
                    binw.Write(mymesh.objs[i].verts[j].z * 2);
                    binw.Write(-(mymesh.objs[i].verts[j].y * 2));
                    binw.Write(mymesh.objs[i].verts[j].nx);
                    binw.Write(mymesh.objs[i].verts[j].nz);
                    binw.Write(mymesh.objs[i].verts[j].ny);
                    binw.Write(mymesh.objs[i].verts[j].u);
                    binw.Write(mymesh.objs[i].verts[j].v);
                    binw.Write(mymesh.objs[i].verts[j].u);
                    binw.Write(mymesh.objs[i].verts[j].v);
                }

                fcursor = (int)fstr.Position;
                fstr.Seek(subpartsoff, SeekOrigin.Begin);
                binw.Write(fcursor - subpartsoff);
                fstr.Seek(fcursor, SeekOrigin.Begin);
                subpartsoff += 32;

                if (backside)
                {
                    for (j = 0; j < mymesh.objs[i].indcount; j++)
                    {
                        binw.Write((short)mymesh.objs[i].indices[j].z);
                        binw.Write((short)mymesh.objs[i].indices[j].y);
                        binw.Write((short)mymesh.objs[i].indices[j].x);
                    }
                }
                else
                {
                    for (j = 0; j < mymesh.objs[i].indcount; j++)
                    {
                        binw.Write((short)mymesh.objs[i].indices[j].x);
                        binw.Write((short)mymesh.objs[i].indices[j].y);
                        binw.Write((short)mymesh.objs[i].indices[j].z);
                    }
                }
            }

            //Change last values
            filesize = (int)fstr.Position;
            fstr.Seek(116, SeekOrigin.Begin);
            binw.Write(filesize - fstveroff);
            binw.Write(fstveroff);
            fstr.Seek(144, SeekOrigin.Begin);
            binw.Write(filesize);

            return fstr;
        }

        private int selectbpp(Bitmap mypic)
        {
            for (int i = 0; i < mypic.Height; i++)
            {
                for (int j = 0; j < mypic.Width; j++)
                {
                    if (mypic.GetPixel(j, i).A < 255)
                    {
                        return 32;
                    }
                }
            }
            return 24;
        }

        public Stream makeimagefile(_texture mytex)
        {
            int i, j, bpp;
            MemoryStream image = new MemoryStream();
            BinaryWriter imbin = new BinaryWriter(image);

            //WE00
            image.WriteByte(87);
            image.WriteByte(69);
            image.WriteByte(48);
            image.WriteByte(48);

            imbin.Write(mytex.id);
            if (mytex.file.EndsWith("dds", StringComparison.CurrentCultureIgnoreCase))
            {
                image.WriteByte(0);
                image.WriteByte(255);
                FileInfo fi = new FileInfo(mytex.file);
                imbin.Write((int)fi.Length);
                imbin.Write((short)0);
                imbin.Write((short)16);

                FileStream ddsload = new FileStream(mytex.file, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[ddsload.Length];
                ddsload.Read(buffer, 0, (int)ddsload.Length);

                image.Write(buffer, 0, (int)buffer.Length);
                ddsload.Close();
            }
            else
            {
                Bitmap pic;
                try
                {
                    pic = new Bitmap(mytex.file);
                }
                catch
                {
                    throw new Exception("Problem while trying to import texture: " + mytex.file + ".");
                }

                bpp = selectbpp(pic);
                image.WriteByte(3);
                image.WriteByte((byte)bpp);

                imbin.Write((short)pic.Width);
                imbin.Write((short)pic.Height);
                imbin.Write((short)0);
                imbin.Write((short)16);

                for (j = 0; j < pic.Height; j++)
                {
                    for (i = 0; i < pic.Width; i++)
                    {
                        image.WriteByte(pic.GetPixel(i, j).R);
                        image.WriteByte(pic.GetPixel(i, j).G);
                        image.WriteByte(pic.GetPixel(i, j).B);
                        if (bpp == 32)
                        {
                            image.WriteByte(pic.GetPixel(i, j).A);
                        }
                    }
                }
            }

            return image;
        }

        private void getsideprops(string sidename, byte[] props)
        {
            props[1] = 6;
            props[3] = 2;

            if (sidename.Split('-').Length < 2)
            {
                switch (sidename)
                {
                    case "UPPER_TRIBUNE":
                        props[0] = 0;
                        props[2] = 7;
                        break;
                    case "RIGHT_TRIBUNE":
                        props[0] = 0;
                        props[2] = 5;
                        break;
                    case "DOWN_TRIBUNE":
                        props[0] = 0;
                        props[2] = 3;
                        break;
                    case "LEFT_TRIBUNE":
                        props[0] = 0;
                        props[2] = 1;
                        break;
                    case "UPPER_SIDE":
                        props[0] = 1;
                        props[2] = 7;
                        break;
                    case "RIGHT_SIDE":
                        props[0] = 1;
                        props[2] = 5;
                        break;
                    case "DOWN_SIDE":
                        props[0] = 1;
                        props[2] = 3;
                        break;
                    case "LEFT_SIDE":
                        props[0] = 1;
                        props[2] = 1;
                        break;
                    case "BASE":
                        props[0] = 0;
                        props[1] = 3;
                        props[2] = 0;
                        break;
                    case "ROOF":
                        props[0] = 1;
                        props[1] = 8;
                        props[2] = 0;
                        break;
                    default:
                        props[0] = 0;
                        props[1] = 3;
                        props[2] = 0;
                        break;
                }
            }
            else
            {
                switch (sidename.Split('_')[0])
                {
                    case "UPPER-LEFT":
                        props[2] = 0;
                        break;
                    case "DOWN-LEFT":
                        props[2] = 2;
                        break;
                    case "DOWN-RIGHT":
                        props[2] = 4;
                        break;
                    case "UPPER-RIGHT":
                        props[2] = 6;
                        break;
                    default:
                        props[2] = 0;
                        break;
                }
                switch (sidename.Split('_')[1])
                {
                    case "TRIBUNE":
                        props[0] = 0;
                        break;
                    case "SIDE":
                        props[0] = 1;
                        break;
                }
            }
        }

        private int crowdid(string side)
        {
            int id;

            switch (side.Split('_')[0])
            {
                case "LEFT":
                    id = 1;
                    break;
                case "DOWN-LEFT":
                    id = 4;
                    break;
                case "DOWN":
                    id = 0;
                    break;
                case "DOWN-RIGHT":
                    id = 7;
                    break;
                case "RIGHT":
                    id = 3;
                    break;
                case "UP-RIGHT":
                    id = 6;
                    break;
                case "UP":
                    id = 2;
                    break;
                case "UP-LEFT":
                    id = 5;
                    break;
                default:
                    id = int.Parse(side);
                    break;
            }

            if (side.Split('_').Length > 1)
            {
                switch (side.Split('_')[1])
                {
                    case "TIE1":
                        id += 8;
                        break;
                    case "TIE2":
                        id += 16;
                        break;
                }
            }

            if (id > 19)
                id = -1;
            return id;
        }

        public void exportcrowd(Stream subfile, string filename)
        {
            int[] ides = new int[] { 0, 1, 2, 3 };

            BinaryReader outread = new BinaryReader(subfile);

            FileStream thesef = new FileStream(filename, FileMode.OpenOrCreate);
            StreamWriter sefstring = new StreamWriter(thesef);


            subfile.Seek(80, SeekOrigin.Begin);

            int numofquads, unk;
            float xx, yy, zz;
            float xxx, yyy;

            for (int i = 0; i < 20; i++)
            {
                numofquads = outread.ReadInt32();
                sefstring.Write(String.Format("\"{0}\" = {1}\n", i, numofquads));
                xx = outread.ReadSingle();
                yy = outread.ReadSingle();
                zz = outread.ReadSingle();
                //sefstring.Write(String.Format("{0:0.000000} {1:0.000000} {2:0.000000}\nSize: ", xx/2, zz/2, yy/2));
                xx = outread.ReadSingle();
                yy = outread.ReadSingle();
                zz = outread.ReadSingle();
                //sefstring.Write(String.Format("{0:0.000000} {1:0.000000} {2:0.000000}\n\n", xx/2, zz/2, yy/2));
                if (numofquads > 0)
                {
                    for (int j = 0; j < numofquads; j++)
                    {
                        outread.ReadInt32();
                        outread.ReadInt32();
                        xxx = outread.ReadSingle();
                        yyy = outread.ReadSingle();
                        unk = outread.ReadInt32();

                        //sefstring.Write(String.Format("\"{0:0.00}-{1:0.00}-{2}\" = 1\n", xxx, yyy, unk));

                        foreach (int k in ides)
                        {
                            xx = outread.ReadSingle();
                            yy = outread.ReadSingle();
                            zz = outread.ReadSingle();
                            sefstring.Write(String.Format("{0:0.000000} {1:0.000000} {2:0.000000}\n", xx / 2, zz / 2, yy / 2));
                        }

                        for (int k = 0; k < 4; k++)
                        {
                            outread.ReadInt32();
                            outread.ReadInt32();
                        }
                    }
                }
            }

            sefstring.Close();
            thesef.Close();
        }

        private float distance3d(_simplevert vert1, _simplevert vert2)
        {
            float dist;

            dist = (float)(Math.Sqrt(Math.Pow(vert2.x - vert1.x, 2) + Math.Pow(vert2.y - vert1.y, 2) + Math.Pow(vert2.z - vert1.z, 2)));

            return dist;
        }

        public Stream importcrowd(_crowd crowd)
        {
            int fhead, fcursor;
            int[] ides = new int[] { 1, 2, 0, 3 };

            MemoryStream output = new MemoryStream();
            BinaryWriter outread = new BinaryWriter(output);

            fhead = 0;
            fcursor = 80;
            output.Seek(80, SeekOrigin.Begin);

            for (int i = 0; i < 20; i++)
            {
                output.Seek(fhead, SeekOrigin.Begin);
                outread.Write(fcursor);
                fhead += 4;
                output.Seek(fcursor, SeekOrigin.Begin);

                if (crowd.sides[i].quads == null)
                {
                    fcursor += 28;
                    if ((output.Length - fcursor) < 0)
                    {
                        for (int j = 0; j < 28; j++)
                        {
                            output.WriteByte(0);
                        }
                    }
                }
                else
                {
                    outread.Write((int)crowd.sides[i].quads.Length);
                    outread.Write(2 * crowd.sides[i].center.x);
                    outread.Write(2 * crowd.sides[i].center.z);
                    outread.Write(-2 * crowd.sides[i].center.y);

                    outread.Write(2 * crowd.sides[i].space.x);
                    outread.Write(2 * crowd.sides[i].space.z);
                    outread.Write(2 * crowd.sides[i].space.y);
                    float dist;
                    byte crowdlines;
                    for (int j = 0; j < crowd.sides[i].quads.Length; j++)
                    {
                        dist = distance3d(crowd.sides[i].quads[j].verts[1], crowd.sides[i].quads[j].verts[0]);
                        crowdlines = (byte)Math.Round(dist / 0.3);
                        output.WriteByte(1);
                        output.WriteByte(crowdlines);
                        if (i == 3)
                        {
                            output.WriteByte(0);
                            output.WriteByte(100);
                        }
                        else if (i == 1)
                        {
                            output.WriteByte(0);
                            output.WriteByte(100);
                        }
                        else
                        {
                            output.WriteByte(100);
                            output.WriteByte(0);
                        }
                        outread.Write(0);
                        outread.Write(crowd.sides[i].lookat.x);
                        outread.Write(-crowd.sides[i].lookat.y);
                        outread.Write(2);

                        foreach (int k in ides)
                        {
                            outread.Write(2 * crowd.sides[i].quads[j].verts[k].x);
                            outread.Write(2 * crowd.sides[i].quads[j].verts[k].z);
                            outread.Write(-2 * crowd.sides[i].quads[j].verts[k].y);
                        }

                        for (int k = 0; k < 8; k++)
                        {
                            outread.Write((float)0.5);
                        }
                    }

                    fcursor = (int)output.Position;
                }
            }

            return output;
        }

        public Image bintodds(Stream input)
        {
            return bintodds(input, false);
        }

        public Image bintodds(Stream input, bool xbox)
        {
            byte[] texname = new byte[2];
            byte[] buffer;
            long tem, tem2;
            ushort tem3, tem4, paloff;
            byte bpp;
            Color[] palette;
            FreeImageAPI.FreeImageBitmap pic;

            EndianBinaryReader inread = new EndianBinaryReader(MiscUtil.Conversion.EndianBitConverter.Little, input);
            if(xbox)
                inread = new EndianBinaryReader(MiscUtil.Conversion.EndianBitConverter.Big, input);

            input.Seek(4, SeekOrigin.Begin);
            input.Read(texname, 0, 2);
            input.Seek(8, SeekOrigin.Begin);
            tem = inread.ReadUInt32();
            input.Seek(7, SeekOrigin.Begin);
            bpp = inread.ReadByte();
            tem3 = inread.ReadUInt16();
            tem4 = inread.ReadUInt16();
            buffer = new byte[3];
            paloff = inread.ReadUInt16();
            tem2 = inread.ReadUInt16();
            input.Read(buffer, 0, 3);
            input.Seek(tem2, SeekOrigin.Begin);

            if (System.Text.ASCIIEncoding.ASCII.GetString(buffer) == "DDS")
            {
                pic = new FreeImageAPI.FreeImageBitmap(input);
            }
            else
            {
                pic = new FreeImageAPI.FreeImageBitmap(tem3, tem4);

                if (bpp == 8)
                {
                    palette = new Color[256];

                    input.Seek(paloff, SeekOrigin.Begin);
                    buffer = new byte[4];
                    for (int i = 0; i < 256; i++)
                    {
                        input.Read(buffer, 0, 4);
                        palette[i] = Color.FromArgb(buffer[3], buffer[0], buffer[1], buffer[2]);
                    }

                    input.Seek(tem2, SeekOrigin.Begin);
                    for (int j = 0; j < tem4; j++)
                    {
                        for (int k = 0; k < tem3; k++)
                        {
                            pic.SetPixel(k, tem4 - j - 1, palette[input.ReadByte()]);
                        }
                    }
                }
                else
                {
                    buffer = new byte[4];
                    for (int j = 0; j < tem4; j++)
                    {
                        for (int k = 0; k < tem3; k++)
                        {
                            buffer[3] = 255;
                            input.Read(buffer, 0, bpp / 8);
                            pic.SetPixel(k, tem4 - j - 1, Color.FromArgb(buffer[3], buffer[0], buffer[1], buffer[2]));
                        }
                    }
                }
            }

            return (Image)pic;
        }
    }
}
