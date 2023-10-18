using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using FTD2XX_NET;

namespace Picky
{
    internal class Control
    {
        static FTDI myFtdiDevice = new FTDI();
        static FTDI.FT_STATUS ftStatus;
        static byte[] sentBytes = new byte[2];
        static uint receivedBytes;

        static Control()
        {
            //Get serial number of device with index 0
            ftStatus = myFtdiDevice.OpenByIndex(0);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return;
            }
            //Reset device
            ftStatus = myFtdiDevice.ResetDevice();
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return;
            }
            //Set Baud Rate
            ftStatus = myFtdiDevice.SetBaudRate(921600);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return;
            }
            //Set Bit Bang
            ftStatus = myFtdiDevice.SetBitMode(255, FTD2XX_NET.FTDI.FT_BIT_MODES.FT_BIT_MODE_SYNC_BITBANG);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return;
            }
            sentBytes[0] = 0x00;
            myFtdiDevice.Write(sentBytes, 1, ref receivedBytes);
            Console.WriteLine("Relay Board Initialized.");

        }

        public void SetIlluminatorOn(bool state)
        {
            if (state == true)
                SetRelay(4);
            else
                ClearRelay(4);
        }

        public void SetPumpOn(bool state)
        {
            if (state == true)
                SetRelay(5);
            else
                ClearRelay(5);
        }
                    
        public void SetRelay(int Relay_Number)
        {
            byte mask = (byte) (0x01 << Relay_Number);
            sentBytes[0] |= mask;
            myFtdiDevice.Write(sentBytes, 1, ref receivedBytes);
            Console.WriteLine("Set Relay: " + Relay_Number + " " + Convert.ToString(sentBytes[0], 2));
        }

        public void ClearRelay(int Relay_Number)
        {
            byte mask = (byte)~(0x01 << Relay_Number);
            sentBytes[0] &= mask;
            myFtdiDevice.Write(sentBytes, 1, ref receivedBytes);
            Console.WriteLine("Clear Relay: " + Relay_Number + " " + Convert.ToString(sentBytes[0], 2) + " " + Convert.ToString(mask, 2));
        }
      
    }
}

