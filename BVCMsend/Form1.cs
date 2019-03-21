using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BVCMsend
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern void keybd_event(uint vk, uint scan, uint flags, uint extraInfo);
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;


        public static bool GetBit(byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
        private static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }
        public static byte ToBcd(int value)
        {
            if (value < 0 || value > 99999999)
                throw new ArgumentOutOfRangeException("value");
            byte ret = new byte();

            ret = (byte)(value % 10);
            value /= 10;
            ret |= (byte)((value % 10) << 4);
            value /= 10;

            return ret;
        }
        int credit = 0;
        int credit_coin = 0;
        bool escrow = false;
        List<byte> last_state = new List<byte>();
        byte[] standby = { 0x02, 0x00, 0x20, 0x58, 0xa7, 0x40, 0x1D, 0x03 };
        byte[] standby_cm = { 0x02, 0x00, 0x20, 0x60, 0xA6, 0xDC, 0x03 };
        byte[] alldata_cm = { 0x02, 0x00, 0x20, 0x61, 0xB6, 0xFD, 0x03 };
        byte[] inreq_cm = { 0x02, 0x00, 0x20, 0x62, 0x86, 0x9E, 0x03 };
        byte[] inreq = { 0x02, 0x00, 0x20, 0x5a, 0xa5, 0x06, 0x3d, 0x03 };
        byte[] alldata = { 0x02, 0x00, 0x20, 0x59, 0x01, 0xA6, 0x03 };
        byte[] reset = { 0x02, 0x00, 0x20, 0xff, 0xd4, 0x8a, 0x03 };
        byte[] disable = { 0x02, 0x00, 0x20, 0x5b, 0x7d, 0xe3, 0x10, 0x00, 0x00, 0x13, 0x3b, 0xd9, 0x03 };
        byte[] insertclear = { 0x02, 0x00, 0x20, 0x5b, 0x7d, 0xe3, 0x10, 0x22, 0x00, 0x31, 0xd7, 0x5f, 0x03 };
        int[] bvcontroldata = { 0x00, 0x00 ,0x00,0x00 ,0x00};
        byte[] edcdata = { 0x02,0x00,0x20,0x5b, 0x07, 0x80, 0x05, 0x01, 0x7d,0xe3,0x01,0x00,0x01,0x80,0x79,0x79, 0x03};
        int[] bvcontroldata_return = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,0x00 };
        int[] cmcontrol1data = new int[4];
        int[] cmcontrol_return_typedata =new int[7];
        public Form1()
        { 
            InitializeComponent();
            foreach (string comport in SerialPort.GetPortNames())               
            {
                comboBox1.Items.Add(comport);              
            }

            bvcm.DataReceived += new SerialDataReceivedEventHandler(EventDataReceived);
            
            datachanged();

        }
        private void EventDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            int recsize = bvcm.BytesToRead;
            byte[] buff_raw = new byte[recsize];
            List<byte> buff = new List<byte>();
            bvcm.Read(buff_raw, 0, recsize);
            
            for(int i=0;i < recsize; i++)
            {
                buff.Add(buff_raw[i]);
            }
            for(int i=1; i < buff.Count-1; i++)
            {
                if(buff[i]==0x7d && buff[i + 1] == 0xe3)
                {
                    buff[i] = 0x03;
                    buff.RemoveAt(i + 1);
                }
                else if (buff[i] == 0x7d && buff[i + 1] == 0xe2)
                {
                    buff[i] = 0x02;
                    buff.RemoveAt(i + 1);
                }
            }
            if(buff.Count > 6)
            {
                if (buff[3] == 0x11)
                {
                    rx.Text = "ACK1";
                    Delay(30);
                    rx.Text = "";
                }
                else if (buff[3] == 0x22)
                {
                    rx.Text = "ACK2";
                    Delay(30);
                    rx.Text = "";
                }
                else if (buff[3] == 0x33)
                {
                    rx.Text = "ACK3";
                    Delay(30);
                    rx.Text = "";
                }
                else if (buff[3] == 0x44)
                {
                    rx.Text = "ACK4";
                    Delay(30);
                    rx.Text = "";
                }
                else if (buff[3] == 0x55)
                {
                    rx.Text = "ACK5";
                    Delay(30);
                    rx.Text = "";
                }
                else if (buff[3] == 0xEE)
                {
                    rx.Text = "NAK";
                    Delay(30);
                    rx.Text = "";
                }
                else if (buff[3] == 0xFF)
                {
                    richTextBox1.Text += "Resetting";
                }
                else if (buff[3] == 0xFD)
                {
                    richTextBox1.Text += "Timeout";
                }
                else if (buff[3] == 254)
                {
                    richTextBox1.Text += "Error";
                }
                else
                {
                    richTextBox1.Text += buff[3].ToString();
                }
                if (buff[3] != 0x11)
                {
                    for (int i = 0; i < buff.Count; i++)
                    {
                        richTextBox1.Text += " " + buff[i].ToString("X2");
                        richTextBox1.Text += " ";
                    }
                    richTextBox1.Text += "\r\n";
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                }
                for (int i = 0; i < buff.Count; i++)
                {
                    if (i > 2)
                    {
                        if (buff[i] == 0x18 && escrow == false)
                        {
                            if (buff[i - 1] == 0x06)
                            {
                                if (buff[i + 5] > 0)
                                {
                                    credit += Convert.ToInt16(buff[i + 5]) * 2000;
                                    escrow = true;
                                }
                                if (buff[i + 1] > 0)
                                {
                                    credit += Convert.ToInt16(buff[i + 1]) * 1000;
                                    escrow = true;
                                }
                                if (buff[i + 9] > 0)
                                {
                                    credit += Convert.ToInt16(buff[i + 7]) * 10000;
                                    escrow = true;
                                }

                            }
                            else if (buff[i - 1] == 0x07)
                            {
                                if (buff[i + 9] > 0)
                                {
                                    credit += Convert.ToInt16(buff[i + 7]) * 10000;
                                    escrow = true;
                                }
                            }
                            else if (buff[i - 1] == 0x05)
                            {
                                if (buff[i + 1] > 0)
                                {
                                    credit += Convert.ToInt16(buff[i + 1]) * 1000;
                                    escrow = true;
                                }
                                if (buff[i + 2] > 0)
                                {
                                    credit += Convert.ToInt16(buff[i + 2]) * 5000;
                                    escrow = true;
                                }
                                if (buff[i + 3] > 0)
                                {
                                    credit += Convert.ToInt16(buff[i + 3]) * 10000;
                                    escrow = true;
                                }

                            }

                        }
                        if(buff[i] == 0x08 && buff[i - 1] == 0x05)
                        {                            
                            credit_coin = (Convert.ToInt32(buff[i + 1].ToString("X2")) * 10) + (Convert.ToInt32(buff[i + 2].ToString("X2")) * 50) + (Convert.ToInt32(buff[i + 3].ToString("X2")) * 100) + (Convert.ToInt32(buff[i + 4].ToString("X2")) * 500);
                        }
                        if (escrow == true)
                        {                            
                            if (buff[i] == 0x1b)
                            {
                                if (GetBit(buff[i + 1], 5))
                                {
                                    escrow = false;
                                    bvcm.Write(insertclear, 0, insertclear.Length);
                                    byte[] laststate = new byte[last_state.Count];
                                    for (int j = 0;j < last_state.Count; j++)
                                    {
                                        laststate[j] = last_state[j];
                                    }
                                    bvcm.Write(laststate, 0, laststate.Length);
                                    
                                }
                            }
                        }

                    }
                }
            }

            
            datachanged();
          
        }
        int getfcc(int bc, int dc, byte[] data)
        {
            int fcc = 0;
            fcc = bc;
            fcc = fcc ^ dc;
            for (int i = 0; i < bc-1; i++)
            {
                fcc = fcc ^ data[i];
            }
            return fcc;
        }
        public static int getCRC(byte[] bytes)
        {
            int crc = 0xFFFF;          // initial value
            int polynomial = 0x1021;   // 0001 0000 0010 0001  (0, 5, 12) 

            foreach (byte b in bytes)
            {
                for (int i = 0; i < 8; i++)
                {
                    Boolean bit = ((b >> (7 - i) & 1) == 1);
                    Boolean c15 = ((crc >> 15 & 1) == 1);
                    crc <<= 1;
                    if (c15 ^ bit) crc ^= polynomial;
                }
            }

            crc &= 0xffff;
            //System.out.println("CRC16-CCITT = " + Integer.toHexString(crc));
            return crc;
        }

        void datachanged()
        {
            credit_text.Text = (credit + credit_coin).ToString() ;
            

        }
        private void button1_Click(object sender, EventArgs e)
        {
  
        }

        private void button2_Click(object sender, EventArgs e)
        {
            bvcm.Write(standby, 0, standby.Length);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bvcontrol_data();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            bvcontrol_data();
        }      

        private void acceptbill1000_CheckedChanged(object sender, EventArgs e)
        {
            bvcontrol_data();
        }

       
        void bvcontrol_data()
        {
            bvcontroldata[0] = 0x03; //BC
            bvcontroldata[1] = 0x10; //DC
            bvcontroldata[2] = (Convert.ToInt16(checkBox11.Checked) << 7)+ (Convert.ToInt16(acceptbill5000.Checked) << 6) + (Convert.ToInt16(acceptbill1000.Checked) << 5)+ (Convert.ToInt16(checkBox10.Checked) << 4) + (Convert.ToInt16(clear.Checked) << 1) + Convert.ToInt16(billaccept.Checked);       //DATA 1Byte
            bvcontroldata[3] = Convert.ToInt16(checkBox12.Checked); //DATA 2 Byte
            byte[] temp = new byte[2];            
            temp[0] = Convert.ToByte(bvcontroldata[2]);
            temp[1] = Convert.ToByte(bvcontroldata[3]);
            bvcontroldata[4] = Convert.ToByte(getfcc(bvcontroldata[0], bvcontroldata[1], temp));
            label4.Text = bvcontroldata[2].ToString("X2");
            textBox1.Text = "FCC= " + bvcontroldata[4].ToString("X2");
        }

        void bvcontrol_data_return()
        {
            bvcontroldata_return[0] = 0x05; //BC
            bvcontroldata_return[1] = 0x11; //DC
            bvcontroldata_return[2] = 0x00; //DATA 1Byte
            bvcontroldata_return[3] = Convert.ToInt16(numericUpDown1.Value) << 4; //DATA 2Byte
            bvcontroldata_return[4] = Convert.ToInt16(numericUpDown2.Value); //DATA 3Byte
            bvcontroldata_return[5] = 0x00; //DATA 4Byte
            byte[] temp = new byte[4];
            for(int i=0; i<4; i++)
            {
                temp[i] = Convert.ToByte(bvcontroldata_return[i+2]);
            }
            bvcontroldata_return[6] = Convert.ToByte(getfcc(bvcontroldata_return[0], bvcontroldata_return[1],temp));
            label1.Text = "FCC = " + bvcontroldata_return[6].ToString("X2");

        }
        void cmcontrol1_data()
        {
            cmcontrol1data[0] = 0x02;//BC
            cmcontrol1data[1] = 0x00;//DC
            cmcontrol1data[2] = (Convert.ToInt16(checkBox8.Checked) << 6) + (Convert.ToInt16(checkBox7.Checked) << 5) + (Convert.ToInt16(checkBox6.Checked) << 4) + (Convert.ToInt16(checkBox5.Checked) << 2) + (Convert.ToInt16(checkBox4.Checked) << 1) + Convert.ToInt16(checkBox3.Checked); //DATA 1byte
            byte[] temp = new byte[1];
            temp[0] = Convert.ToByte(cmcontrol1data[2]);
            cmcontrol1data[3] = Convert.ToByte(getfcc(cmcontrol1data[0], cmcontrol1data[1], temp));
            label11.Text = "FCC = " + cmcontrol1data[3].ToString("X2");
        }
        void cmcontrol_return_type()
        {
            cmcontrol_return_typedata[0] = 0x05; //BC
            cmcontrol_return_typedata[1] = 0x02; //DC
            cmcontrol_return_typedata[2] = ToBcd(Convert.ToInt16(numericUpDown6.Value)); 
            cmcontrol_return_typedata[3] = ToBcd(Convert.ToInt16(numericUpDown5.Value));
            cmcontrol_return_typedata[4] = ToBcd(Convert.ToInt16(numericUpDown4.Value));
            cmcontrol_return_typedata[5] = ToBcd(Convert.ToInt16(numericUpDown3.Value));
            byte[] temp = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                temp[i] = Convert.ToByte(cmcontrol_return_typedata[i+2]);
            }
            cmcontrol_return_typedata[6] = Convert.ToByte(getfcc(cmcontrol_return_typedata[0], cmcontrol_return_typedata[1], temp));
            label12.Text = "FCC = " + cmcontrol_return_typedata[6];
          }
        private void button3_Click(object sender, EventArgs e)
        {
            if (clear.Checked)
            {
                escrow = false;
                credit = 0;
            }
                
            last_state.Clear();
            List<byte> packet = new List<byte>();
            byte[] CRC = new byte[2];
            byte[] Rawdata = { 0x00,0x20,0x5b,0x03, 0x10, Convert.ToByte(bvcontroldata[2]),0x00, Convert.ToByte(bvcontroldata[4]) };
            packet.Add(0x02); //start bit
            packet.Add(0x00); //sid
            packet.Add(0x20); //rid
            packet.Add(0x5b); //command
            packet.Add(0x7d); //esc
            packet.Add(0xe3); //esc_etx
            for(int i=1; i < bvcontroldata.Length; i++)
            {
                packet.Add(Convert.ToByte(bvcontroldata[i]));
            }
            
           CRC = BitConverter.GetBytes(getCRC(Rawdata));
            packet.Add(CRC[1]);
            packet.Add(CRC[0]);
            for (int i = 3; i < packet.Count; i++)
            {
                if (packet[i] == 0x03)
                {
                    packet[i] = 0x7D;
                    packet.Insert(i + 1, 0xE3);
                }
                else if (packet[i] == 0x02)
                {
                    packet[i] = 0x7D;
                    packet.Insert(i + 1, 0xE2);
                }
            }
            packet.Add(0x03); //end bit
            byte[] sendpacketdata = new byte[packet.Count];
            for(int i = 0; i < packet.Count; i++)
            {
                sendpacketdata[i] = packet[i];
                richTextBox1.Text += packet[i].ToString("X2") + " ";
            }
            bvcm.Write(sendpacketdata, 0, sendpacketdata.Length);
            for(int i=0; i < sendpacketdata.Length; i++)
            {
                last_state.Add(sendpacketdata[i]);
            }
        }
        private void button15_Click(object sender, EventArgs e)
        {
            List<byte> packet = new List<byte>();
            byte[] CRC = new byte[2];
            byte[] Rawdata = { 0x00, 0x20, 0x63, (byte)cmcontrol_return_typedata[0], (byte)cmcontrol_return_typedata[1], (byte)cmcontrol_return_typedata[2], (byte)cmcontrol_return_typedata[3], (byte)cmcontrol_return_typedata[4], (byte)cmcontrol_return_typedata[5], (byte)cmcontrol_return_typedata[6] };
            packet.Add(0x02); //start bit
            packet.Add(0x00); //SID
            packet.Add(0x20); //RID
            packet.Add(0x63); //Command
            for (int i = 0; i < cmcontrol_return_typedata.Length; i++)
            {
                packet.Add(Convert.ToByte(cmcontrol_return_typedata[i]));
            }
            CRC = BitConverter.GetBytes(getCRC(Rawdata));
            packet.Add(CRC[1]);
            packet.Add(CRC[0]);
            for (int i = 3; i < packet.Count; i++)
            {
                if (packet[i] == 0x03)
                {
                    packet[i] = 0x7D;
                    packet.Insert(i + 1, 0xE3);
                }
                else if (packet[i] == 0x02)
                {
                    packet[i] = 0x7D;
                    packet.Insert(i + 1, 0xE2);
                }
            }
            packet.Add(0x03); //end bit
            byte[] sendpacketdata = new byte[packet.Count];
            for (int i = 0; i < packet.Count; i++)
            {
                sendpacketdata[i] = packet[i];
                richTextBox1.Text += packet[i].ToString("X2") + " ";
            }
            richTextBox1.Text += "\r\n";
            bvcm.Write(sendpacketdata, 0, sendpacketdata.Length);
        }
        private void button11_Click(object sender, EventArgs e)
        {
            List<byte> packet = new List<byte>();
            byte[] CRC = new byte[2];
            byte[] Rawdata = { 0x00, 0x20, 0x63, (byte)cmcontrol1data[0], (byte)cmcontrol1data[1], (byte)cmcontrol1data[2], (byte)cmcontrol1data[3] };
            packet.Add(0x02); //start bit
            packet.Add(0x00); //SID
            packet.Add(0x20); //RID
            packet.Add(0x63); //Command
            for (int i = 0; i < cmcontrol1data.Length; i++)
            {
                packet.Add(Convert.ToByte(cmcontrol1data[i]));
            }
            CRC = BitConverter.GetBytes(getCRC(Rawdata));
            packet.Add(CRC[1]);
            packet.Add(CRC[0]);
            for (int i = 3; i < packet.Count; i++)
            {
                if (packet[i] == 0x03)
                {
                    packet[i] = 0x7D;
                    packet.Insert(i + 1, 0xE3);
                }
                else if (packet[i] == 0x02)
                {
                    packet[i] = 0x7D;
                    packet.Insert(i + 1, 0xE2);
                }                
            }
            packet.Add(0x03); //end bit
            byte[] sendpacketdata = new byte[packet.Count];
            for (int i = 0; i < packet.Count; i++)
            {
                sendpacketdata[i] = packet[i];
                richTextBox1.Text +=  packet[i].ToString("X2") + " ";
            }
            richTextBox1.Text += "\r\n";
            bvcm.Write(sendpacketdata, 0, sendpacketdata.Length);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            escrow = false;
            bool timer = false;
            //if (timer1.Enabled)
           // {
            //    timer1.Stop();
           //     timer = true;
          //  }                          
            List<byte> packet = new List<byte>();
            byte[] CRC = new byte[2];
            byte[] Rawdata = { 0x00, 0x20, 0x5b, Convert.ToByte(bvcontroldata_return[0]),
            Convert.ToByte(bvcontroldata_return[1]),Convert.ToByte(bvcontroldata_return[2]),
            Convert.ToByte(bvcontroldata_return[3]),Convert.ToByte(bvcontroldata_return[4]),
            Convert.ToByte(bvcontroldata_return[5]),Convert.ToByte(bvcontroldata_return[6])};
            packet.Add(0x02); //start bit
            packet.Add(0x00); //sid
            packet.Add(0x20); //rid
            packet.Add(0x5b); //command
            for(int i=0; i < Rawdata.Length; i++)
            {
                if(i > 2)
                    packet.Add(Rawdata[i]);
            }
            CRC = BitConverter.GetBytes(getCRC(Rawdata));
            packet.Add(CRC[1]);
            packet.Add(CRC[0]);
            packet.Add(0x03);
            byte[] sendpacketdata = new byte[packet.Count];
            for (int i = 0; i < packet.Count; i++)
            {
                sendpacketdata[i] = packet[i];
                richTextBox1.Text += packet[i].ToString("X2") + " ";
            }
            bvcm.Write(disable, 0, disable.Length);
            Delay(500);
            
            bvcm.Write(sendpacketdata, 0, sendpacketdata.Length);
            byte[] laststate = new byte[last_state.Count];
            for(int i=0; i< last_state.Count; i++)
            {
                laststate[i] = last_state[i];
            }
            Delay(500);
            bvcm.Write(insertclear, 0, insertclear.Length);
            bvcm.Write(laststate, 0, laststate.Length);
            if (timer)
            {
                timer1.Start();
            }



        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bvcm.Write(inreq,0,inreq.Length);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            timer1.Stop();
            timer2.Stop();
            bvcm.Write(reset, 0, reset.Length);
            credit = 0;
            datachanged();

        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            bvcontrol_data_return();
            if (checkBox1.Checked)
                numericUpDown1.Enabled = true;
            else
            {
                numericUpDown1.Value = 0;
                numericUpDown1.Enabled = false;
            }
            
        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            bvcontrol_data_return();
            if (checkBox2.Checked)
                numericUpDown2.Enabled = true;
            else
            {
                numericUpDown2.Value = 0;
                numericUpDown2.Enabled = false;
            }
                
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            bvcontrol_data_return();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            bvcontrol_data_return();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            bvcm.Write(alldata, 0, alldata.Length);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            bvcm.Write(alldata_cm, 0, standby_cm.Length);
        }

        private void tabPage4_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            bvcm.Write(standby_cm, 0, standby_cm.Length);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            cmcontrol1_data();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            cmcontrol1_data();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            cmcontrol1_data();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            cmcontrol1_data();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            cmcontrol1_data();
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            cmcontrol1_data();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            timer2.Start();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            timer2.Stop();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            bvcm.Write(inreq_cm, 0, inreq_cm.Length);
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            cmcontrol_return_type();
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            cmcontrol_return_type();
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            cmcontrol_return_type();
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            cmcontrol_return_type();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            label13.Text = richTextBox1.TextLength.ToString();
            if (richTextBox1.TextLength > 2000)
                richTextBox1.Clear();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            bvcm.PortName = comboBox1.Text;
            bvcm.Open();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            timer2.Stop();
            bvcm.Close();
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            bvcontrol_data();
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            bvcontrol_data();
        }

        private void acceptbill5000_CheckedChanged(object sender, EventArgs e)
        {
            bvcontrol_data();
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            bvcontrol_data();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            bvcm.Write(edcdata, 0, edcdata.Length);
        }

        private void bvcm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

        }
    }
   
      

      
    
}
