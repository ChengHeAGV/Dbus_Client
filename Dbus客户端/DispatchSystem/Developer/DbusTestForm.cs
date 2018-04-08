using System;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DispatchSystem.Developer
{
    public partial class DbusTestForm : Form
    {
        int LocalAddress;
        int TargetAddress;
        EndPoint endPoint;
        int deviceNum = 0;
        Thread th;
        public DbusTestForm()
        {
            InitializeComponent();
        }
        string[] datekey = new string[10];
        private void DbusTestForm_Load(object sender, EventArgs e)
        {
            textBoxTargetIp.Text = UdpSever.ipaddress.ToString();

            LocalAddress = int.Parse(textBoxLocalAddress.Text);
            TargetAddress = int.Parse(textBoxTargetAddress.Text);

            IPAddress IPadr = IPAddress.Parse(textBoxTargetIp.Text);//先把string类型转换成IPAddress类型
            endPoint = new IPEndPoint(IPadr, int.Parse(textBoxTargetPort.Text));//传递IPAddress和Port

            //心跳
            Thread th1 = new Thread(Heart);
            th1.Start();
            //写单个寄存器
            Thread th2 = new Thread(WriteRegister);
            th2.Start();


            deviceNum = int.Parse(textBoxLocalAddress.Text);

            datekey[0] = "寄存器";
            datekey[1] = "时间戳";
            datekey[2] = "十进制";
            datekey[3] = "十六进制";
            datekey[4] = "二进制";
            datekey[5] = "字符串";

            doubleBufferListView1.FullRowSelect = true;//要选择就是一行
            doubleBufferListView1.Columns.Add(datekey[0], 80, HorizontalAlignment.Center);
            doubleBufferListView1.Columns.Add(datekey[1], 230, HorizontalAlignment.Center);
            doubleBufferListView1.Columns.Add(datekey[2], 100, HorizontalAlignment.Center);
            doubleBufferListView1.Columns.Add(datekey[3], 100, HorizontalAlignment.Center);
            doubleBufferListView1.Columns.Add(datekey[4], 200, HorizontalAlignment.Center);
            doubleBufferListView1.Columns.Add(datekey[5], 100, HorizontalAlignment.Center);

            //加载数据
            for (int i = 0; i < UdpSever.RegisterNum; i++)
            {
                ListViewItem item = new ListViewItem();
                item.Text = i.ToString();//"寄存器"
                item.SubItems.Add(UdpSever.Register[deviceNum, i, 1].ToString());//"时间戳"
                item.SubItems.Add(UdpSever.Register[deviceNum, i, 0].ToString());//"十进制"
                item.SubItems.Add(UdpSever.Register[deviceNum, i, 0].ToString("X2"));// "十六进制"
                item.SubItems.Add(Convert.ToString(UdpSever.Register[deviceNum, i, 0], 2).PadLeft(16, '0'));//"二进制"
                byte[] bt = new byte[2];
                bt[0] = (byte)(UdpSever.Register[deviceNum, i, 0] >> 8);
                bt[1] = (byte)(UdpSever.Register[deviceNum, i, 0]);
                string str = Encoding.GetEncoding("GB2312").GetString(bt, 0, 2).Replace("\0", "");
                item.SubItems.Add(str);//"字符串"
                doubleBufferListView1.Items.Add(item);
            }
            //启动自动更新进程
            th = new Thread(fun);
            th.Start();
        }

        private void fun()
        {
            while (true)
            {
                Thread.Sleep(50);
                this.Invoke(new MethodInvoker(delegate
                {
                    //更新数据
                    for (int i = 0; i < UdpSever.RegisterNum; i++)
                    {//UdpSever.Ddata[deviceNum, i, 1].ToString();
                        doubleBufferListView1.Items[i].SubItems[1].Text = UdpSever.StampToString(UdpSever.Register[deviceNum, i, 1]);//时间戳
                        doubleBufferListView1.Items[i].SubItems[2].Text = UdpSever.Register[deviceNum, i, 0].ToString();//十进制
                        doubleBufferListView1.Items[i].SubItems[3].Text = UdpSever.Register[deviceNum, i, 0].ToString("X2");//十六进制
                        doubleBufferListView1.Items[i].SubItems[4].Text = Convert.ToString(UdpSever.Register[deviceNum, i, 0], 2).PadLeft(16, '0');//二进制
                        byte[] bt = new byte[2];
                        bt[0] = (byte)(UdpSever.Register[deviceNum, i, 0] >> 8);
                        bt[1] = (byte)(UdpSever.Register[deviceNum, i, 0]);
                        string str = Encoding.GetEncoding("GB2312").GetString(bt, 0, 2).Replace("\0", "");
                        doubleBufferListView1.Items[i].SubItems[5].Text = str;//ASCII字符串

                        if (i % 2 == 0)
                            doubleBufferListView1.Items[i].BackColor = Color.FromArgb(200, 0xf5, 0xf6, 0xeb);
                    }
                }));
            }
        }

        //心跳
        private void Heart()
        {
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    if (checkBoxHeart.Checked)
                    {
                        int time = int.Parse(textBoxTimeHeart.Text);
                        if (time > 1)
                        {
                            Thread.Sleep(time);
                        }
                        UdpSever.Heart(LocalAddress, TargetAddress, endPoint);
                        lbLedHeart.Trigger = true;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        //基本参数变化时触发
        private void Base_TextChanged(object sender, EventArgs e)
        {
            try
            {
                LocalAddress = int.Parse(textBoxLocalAddress.Text);
                TargetAddress = int.Parse(textBoxTargetAddress.Text);
                deviceNum = int.Parse(textBoxLocalAddress.Text);

                IPAddress IPadr = IPAddress.Parse(textBoxTargetIp.Text);//先把string类型转换成IPAddress类型
                endPoint = new IPEndPoint(IPadr, int.Parse(textBoxTargetPort.Text));//传递IPAddress和Port
            }
            catch
            {
            }
        }

        #region 写单个寄存器
        UInt16 WriterRegisterValue = 0;
        Random rd = new Random();
        string WriteRegisterState = "固定值";
        private void WriteRegister()
        {
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    if (checkBoxWriteRegister.Checked)
                    {
                        int time = int.Parse(textBoxWriteRegisterTime.Text);
                        if (time > 1)
                        {
                            Thread.Sleep(time);
                        }
                        UInt16 value = 0;
                        switch (WriteRegisterState)
                        {
                            case "固定值":
                                value = UInt16.Parse(textBoxWriteRegisterValue.Text);
                                break;
                            case "随机数":
                                value = (ushort)rd.Next(0, 65535);
                                break;
                            case "顺序循环":
                                value = WriterRegisterValue++;
                                break;
                            default:
                                break;
                        }
                        UdpSever.Write_Register(LocalAddress, TargetAddress, int.Parse(textBoxWriteRegisterAddress.Text), value, endPoint);
                        lbLedWriteRegister.Trigger = true;
                    }
                }
                catch //(Exception)
                {

                    // throw;
                }
            }
        }
        private void comboBoxWriteRegister_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBoxWriteRegister.Text)
            {
                case "固定值":
                    textBoxWriteRegisterValue.ReadOnly = false;
                    break;
                case "随机数":
                    textBoxWriteRegisterValue.ReadOnly = true;
                    break;
                case "顺序循环":
                    textBoxWriteRegisterValue.ReadOnly = true;
                    WriterRegisterValue = 0;
                    break;
                default:
                    break;
            }
            WriteRegisterState = comboBoxWriteRegister.Text;
        }
        #endregion

        private void button_write_Click(object sender, EventArgs e)
        {
            UInt16 value = 0;
            switch (WriteRegisterState)
            {
                case "固定值":
                    value = UInt16.Parse(textBoxWriteRegisterValue.Text);
                    break;
                case "随机数":
                    value = (ushort)rd.Next(0, 65535);
                    break;
                case "顺序循环":
                    value = WriterRegisterValue++;
                    break;
                default:
                    break;
            }
            UdpSever.Write_Register(LocalAddress, TargetAddress, int.Parse(textBoxWriteRegisterAddress.Text), value, endPoint);
            lbLedWriteRegister.Trigger = true;
        }

        private void doubleBufferListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (doubleBufferListView1.SelectedItems.Count > 0)
            {
                textBoxWriteRegisterAddress.Text = doubleBufferListView1.SelectedItems[0].SubItems[0].Text;
                textBoxWriteRegisterValue.Text = doubleBufferListView1.SelectedItems[0].SubItems[2].Text;
            }
        }
    }
}
