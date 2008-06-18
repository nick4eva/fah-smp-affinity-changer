using System;
using System.Diagnostics;

namespace FahSmpAffinityChanger
{
    class AffinityChanger
    {
        #region Свойства

        #region Количество процессоров
        /// <summary>
        /// Количество процессоров
        /// </summary>
        public int CpuCount
        {
            get;
            private set;
        } 
        #endregion

        #endregion

        #region Методы

        #region Конструктор
        /// <summary>
        /// Конструктор
        /// </summary>
        public AffinityChanger()
        {
            CpuCount = Environment.ProcessorCount;
        }
        #endregion

        #region Установка маски соответствия процессоров для процесса
        /// <summary>
        /// Установка маски соответствия процессоров для процесса
        /// </summary>
        /// <param name="process"></param>
        /// <param name="cpuNumber"></param>
        public void SetAffinity(Process process, int cpuNumber)
        {
            // если маска соответствия процессоров для текущего процесса не соответствует текущему процессу
            if (process.ProcessorAffinity != (IntPtr)(Math.Pow(2, cpuNumber)))
            {
                // выставляем соответствующую маску соответствия процессоров для текущего процесса
                process.ProcessorAffinity = (IntPtr)(Math.Pow(2, cpuNumber));
            }
        }
        #endregion 

        #endregion
    }
}
