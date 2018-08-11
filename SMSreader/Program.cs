using System;
using System.Text;
using System.IO.Ports;
using SMSPDULib;

namespace TestSMS_2
{
    class Program
    {
        static Modem modem = null;

        static void Main(string[] args)
        {
            string port = Console.ReadLine();
            Modem modem = new Modem("COM" + port);

            modem.GetCIMI();

            modem.CheckMessage();

            modem.Stop();

            Console.ReadLine();
        }
    }

    class Modem
    {
        SerialPort port;
        bool isError = false;

        public Modem(string comPort)
        {
            port = new SerialPort();

            port.BaudRate = 2400; // еще варианты 4800, 9600, 28800 или 56000
            port.DataBits = 7; // еще варианты 8, 9

            port.StopBits = StopBits.One; // еще варианты StopBits.Two StopBits.None или StopBits.OnePointFive         
            port.Parity = Parity.Odd; // еще варианты Parity.Even Parity.Mark Parity.None или Parity.Space

            port.ReadTimeout = 500; // самый оптимальный промежуток времени
            port.WriteTimeout = 500; // самый оптимальный промежуток времени

            port.Encoding = Encoding.GetEncoding("windows-1251");
            port.PortName = comPort;

            // незамысловатая конструкция для открытия порта
            if (port.IsOpen)
                port.Close(); // он мог быть открыт с другими параметрами
            try
            {
                port.Open();
            }
            catch (Exception e)
            {
                isError = true;
                Console.WriteLine("Connecting error!");
            }

            port.DataReceived += ReadData;
        }

        public void Stop()
        {
            if (isError) return;
            port.Close();
        }

        private void ReadData(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string data = sp.ReadExisting();

            //Console.WriteLine(data);

            if (data.IndexOf("CMGL:", 0) != -1)
            {
                int i = 0, end;
                while ((i = data.IndexOf("CMGL:", i)) != -1)
                {
                    if (data.IndexOf("\n", i) == -1) break;

                    i = data.IndexOf("\n", i) + 1;
                    end = (data.IndexOf("\n", i) == -1 ? data.Length : data.IndexOf("\n", i)) - (i + 1);
                    string msg = data.Substring(i, end);

                    try
                    {
                        SMS sms = new SMS();
                        SMS.Fetch(sms, ref msg);

                        Console.WriteLine(sms.PhoneNumber + " - " + sms.Message);
                    } 
                    catch (Exception ee)
                    {
                        Console.WriteLine("Broken msg!");
                    }
                }
            }

            if (data.IndexOf("+CMGL", 0) != -1)
            {
                Console.WriteLine("Msg:");
            }

            if (data.IndexOf("CIMI", 0) != -1)
            {
                int i = data.IndexOf("\n", 0) + 1;
                data = data.Substring(i, data.IndexOf("\n", i) - (i + 1));
                Console.WriteLine(data + " - CIMI\n");
            }
        }

        public void GetCIMI()
        {
            if (isError) return;

            port.Write("AT+CIMI" + "\r\n");
            System.Threading.Thread.Sleep(500);
        }

        public void CheckMessage()
        {
            if (isError) return;

            port.Write("AT+CMGF=0" + "\r\n");
            System.Threading.Thread.Sleep(500);

            port.Write("AT+CPMS =\"SM\"" + "\r\n");
            System.Threading.Thread.Sleep(500);

            port.Write("AT+CMGL=4" + "\r\n");
            System.Threading.Thread.Sleep(500);
        }
    }
}
