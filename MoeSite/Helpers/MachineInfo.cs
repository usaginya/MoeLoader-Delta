using System;
using System.Management;

namespace MoeLoaderDelta.Helpers
{
    public static class MachineInfo
    {
        /// <summary>
        /// 获取主板序列号
        /// </summary>
        /// <returns></returns>
        public static string GetBLOSSerialNumber()
        {
            string sBIOSSerialNumber = string.Empty;
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_BIOS");
                foreach (ManagementObject mo in searcher.Get())
                {
                    sBIOSSerialNumber = mo["SerialNumber"].ToString().Trim();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
            return sBIOSSerialNumber;
        }

        /// <summary>
        /// 获取CPU序列号
        /// </summary>
        /// <returns></returns>
        public static string GetCPUSerialNumber()
        {
            string Cpu = string.Empty;
            try
            {
                ManagementObjectSearcher MOS = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (ManagementObject mo in MOS.Get())
                {
                    Cpu = mo["ProcessorId"].ToString().Trim();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
            return Cpu;
        }

    }
}
