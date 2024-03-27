using System;

namespace TrollBlocker
{
    public class Utils
    {
        public static int GetNumericalDateOfTheDay()
        {
            return int.Parse(DateTime.Today.ToString("ddMMyyyy"));
        }
    }
}
