using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MarthaIsDeadTextTool
{
    public partial class Form1 : Form
    {
        // Very important point:
        //Note that I made this tool by spending a lot of time and checking game files.
        //There is nothing wrong with editing this tool for personal use,
        //BUT DO NOT TRY TO COPY THE CODE AND SELL IT TO OTHERS!
        //NoobInCoding :)
        public struct Header
        {
            public byte[] Unknown;
            public int TextCount;
            public byte[] StillUnknown;
            public Texts[] Texts;
        }
        public struct Texts
        {
            public Line[] Line;
            public int SpeakerLen;
            public string Speaker;
        }

        public struct Line
        {
            public int StrLen;
            public string Str;
            public byte[] FotLine;
        }

        public Form1()
        {
            InitializeComponent();
        }
        public string ClearStr(string str, bool Clear)
        {

            if (Clear)
            {
                str = str.Replace("\r\n", "<cf>");
                str = str.Replace("\n", "<lf>");
                str = str.Replace("\r", "<cr>");
            }
            else
            {
                str = str.Replace("<cf>", "\r\n");
                str = str.Replace("<lf>", "\n");
                str = str.Replace("<cr>", "\r");
            }
            return str;
        }
        public static byte[] CombBytes(byte[] bArray, byte[] newByte)
        {
            byte[] bytes = new byte[bArray.Length + newByte.Length];
            Buffer.BlockCopy(bArray, 0, bytes, 0, bArray.Length);
            Buffer.BlockCopy(newByte, 0, bytes, bArray.Length, newByte.Length);
            return bytes;
        }
        public Header MakeHeader(string path)
        {
            using (BinaryReader ws = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                Header header = new Header
                {
                    Unknown = ws.ReadBytes(125),
                    TextCount = ws.ReadInt32(),
                };
                header.Texts = new Texts[header.TextCount];
                for (int i = 0; i < header.Texts.Length; i++)
                {
                    ws.ReadBytes(33);
                    header.Texts[i].Line = new Line[12];
                    for (int i2 = 0; i2 < header.Texts[i].Line.Length; i2++)
                    {

                        header.Texts[i].Line[i2].StrLen = ws.ReadInt32();
                        if (header.Texts[i].Line[i2].StrLen < 0)
                        {
                            header.Texts[i].Line[i2].StrLen = (int)(header.Texts[i].Line[i2].StrLen ^ 0xFFFFFFFF) * 2;
                            header.Texts[i].Line[i2].Str = ClearStr(Encoding.Unicode.GetString(ws.ReadBytes(header.Texts[i].Line[i2].StrLen)), true);
                            ws.BaseStream.Position += 2;
                        }
                        else if (header.Texts[i].Line[i2].StrLen > 0)
                        {
                            header.Texts[i].Line[i2].Str = ClearStr(Encoding.UTF8.GetString(ws.ReadBytes(header.Texts[i].Line[i2].StrLen - 1)), true);
                            ws.BaseStream.Position++;
                        }
                        header.Texts[i].Line[i2].FotLine = ws.ReadBytes(25);
                    }
                    header.Texts[i].SpeakerLen = ws.ReadInt32();
                    if (header.Texts[i].SpeakerLen < 0)
                    {
                        header.Texts[i].SpeakerLen = (int)(header.Texts[i].SpeakerLen ^ 0xFFFFFFFF) * 2;
                        header.Texts[i].Speaker = ClearStr(Encoding.Unicode.GetString(ws.ReadBytes(header.Texts[i].SpeakerLen)), true);
                        ws.BaseStream.Position += 2;
                    }
                    else if (header.Texts[i].SpeakerLen > 0)
                    {
                        header.Texts[i].Speaker = ClearStr(Encoding.UTF8.GetString(ws.ReadBytes(header.Texts[i].SpeakerLen - 1)), true);
                        ws.BaseStream.Position++;
                    }
                    ws.BaseStream.Seek(8, SeekOrigin.Current);
                }
                return header;

            }
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog Fold = new OpenFileDialog
            {
                Filter = "UEXP File|*.uexp"
            };
            if (Fold.ShowDialog() == DialogResult.OK)
            {
                string Str = "";
                int ind = 0;
                bool Speak = false;
                Header H = MakeHeader(Fold.FileName);
                ind = comboBox1.SelectedIndex;
                foreach (Texts T in H.Texts)
                {
                    if (T.Line[ind].StrLen != 0) Str += T.Line[comboBox1.SelectedIndex].Str + Environment.NewLine;
                    if (Speak && T.SpeakerLen != 0) Str += T.Speaker + Environment.NewLine;
                }
                File.WriteAllText(Fold.FileName + ".txt", Str);
                MessageBox.Show("Done!");
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog Fil = new OpenFileDialog
            {
                Filter = "Text File |*.txt"
            };
            if (Fil.ShowDialog() == DialogResult.OK)
            {

                int k = 0;
                string[] Str = File.ReadAllLines(Fil.FileName);
                string uexp = Path.GetDirectoryName(Fil.FileName) + "\\" + Path.GetFileNameWithoutExtension(Fil.FileName);
                string newpath = uexp + "_new";

                using (BinaryReader ws = new BinaryReader(File.Open(uexp, FileMode.Open)))
                {
                    using (BinaryWriter bw = new BinaryWriter(File.Open(newpath, FileMode.OpenOrCreate)))
                    {
                        Header header = new Header
                        {
                            Unknown = ws.ReadBytes(125)
                        };
                        bw.Write(header.Unknown);
                        header.TextCount = ws.ReadInt32();
                        bw.Write(BitConverter.GetBytes(header.TextCount));
                        header.Texts = new Texts[header.TextCount];
                        for (int i = 0; i < header.Texts.Length; i++)
                        {
                            bw.Write(ws.ReadBytes(24));
                            header.Texts[i].Line = new Line[12];
                            for (int i2 = 0; i2 < header.Texts[i].Line.Length; i2++)
                            {
                                byte[] OldText = { };
                                int TextbyteSize = ws.ReadInt32();
                                OldText = CombBytes(OldText, BitConverter.GetBytes(TextbyteSize));
                                OldText = CombBytes(OldText, ws.ReadBytes(5));
                                header.Texts[i].Line[i2].StrLen = ws.ReadInt32();
                                OldText = CombBytes(OldText, BitConverter.GetBytes(header.Texts[i].Line[i2].StrLen));
                                if (header.Texts[i].Line[i2].StrLen < 0)
                                {
                                    header.Texts[i].Line[i2].StrLen = (int)(header.Texts[i].Line[i2].StrLen ^ 0xFFFFFFFF) * 2;
                                    byte[] Strbytes = ws.ReadBytes(header.Texts[i].Line[i2].StrLen);
                                    OldText = CombBytes(OldText, Strbytes);
                                    header.Texts[i].Line[i2].Str = Encoding.Unicode.GetString(Strbytes);
                                    OldText = CombBytes(OldText, ws.ReadBytes(2));
                                }
                                else if (header.Texts[i].Line[i2].StrLen > 0)
                                {
                                    byte[] Strbytes = ws.ReadBytes(header.Texts[i].Line[i2].StrLen - 1);
                                    OldText = CombBytes(OldText, Strbytes);
                                    header.Texts[i].Line[i2].Str = Encoding.UTF8.GetString(Strbytes);
                                    OldText = CombBytes(OldText, ws.ReadBytes(1));
                                }
                                if (i2 == comboBox1.SelectedIndex && header.Texts[i].Line[i2].StrLen != 0)
                                {
                                    byte[] Bytes = { 0x00, 0x00, 0x00, 0x00, 0x00 };
                                    byte[] NewTexts = CombBytes(Encoding.Unicode.GetBytes(ClearStr(Str[k], false)), new byte[] { 0x00, 0x00 });
                                    bw.Write(BitConverter.GetBytes(NewTexts.Length + 4));
                                    bw.Write(Bytes);
                                    bw.Write(BitConverter.GetBytes(-Convert.ToInt32(NewTexts.Length / 2)));
                                    bw.Write(NewTexts);
                                    k++;
                                }
                                else
                                {
                                    bw.Write(OldText);
                                }
                                header.Texts[i].Line[i2].FotLine = ws.ReadBytes(16);
                                bw.Write(header.Texts[i].Line[i2].FotLine);

                            }
                            header.Texts[i].SpeakerLen = ws.ReadInt32();
                            byte[] OldSpeaker = { };
                            OldSpeaker = CombBytes(OldSpeaker, BitConverter.GetBytes(header.Texts[i].SpeakerLen));
                            if (header.Texts[i].SpeakerLen < 0)
                            {
                                header.Texts[i].SpeakerLen = (int)(header.Texts[i].SpeakerLen ^ 0xFFFFFFFF) * 2;
                                byte[] SpkBytes = ws.ReadBytes(header.Texts[i].SpeakerLen);
                                OldSpeaker = CombBytes(OldSpeaker, SpkBytes);
                                header.Texts[i].Speaker = ClearStr(Encoding.Unicode.GetString(SpkBytes), true);
                                OldSpeaker = CombBytes(OldSpeaker, ws.ReadBytes(2));
                            }
                            else if (header.Texts[i].SpeakerLen > 0)
                            {
                                byte[] SpkBytes = ws.ReadBytes(header.Texts[i].SpeakerLen - 1);
                                OldSpeaker = CombBytes(OldSpeaker, SpkBytes);
                                header.Texts[i].Speaker = ClearStr(Encoding.UTF8.GetString(SpkBytes), true);
                                OldSpeaker = CombBytes(OldSpeaker, ws.ReadBytes(1));
                            }
                            bw.Write(OldSpeaker);
                            bw.Write(ws.ReadBytes(13));
                        }
                        bw.Write(ws.ReadBytes(4));
                        if (!File.Exists(uexp.Replace(".uexp", ".uasset")))
                        {
                            MessageBox.Show("Error : Uasset File Not found!");
                        }
                        else
                        {
                            File.WriteAllBytes(uexp.Replace(".uexp", ".uasset") + "_new", File.ReadAllBytes(uexp.Replace(".uexp", ".uasset")));
                            BinaryWriter AssetFile = new BinaryWriter(File.Open(uexp.Replace(".uexp", ".uasset") + "_new", FileMode.Open));
                            AssetFile.BaseStream.Position = AssetFile.BaseStream.Length - 96;
                            byte[] fo = BitConverter.GetBytes(bw.BaseStream.Length - 4);
                            AssetFile.Write(fo, 0, fo.Length);
                            AssetFile.Close();
                        }

                        bw.Flush();
                        bw.Close();
                    }

                }

                MessageBox.Show("Done!");

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
        }
    }
}
