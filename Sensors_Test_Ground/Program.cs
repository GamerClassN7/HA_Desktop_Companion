// See https://aka.ms/new-console-template for more information
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;

Console.WriteLine("Hello, World :D !");


string name = "Processor";
string category = "% Processor Time";
string instance = "_Total";
int maxSampleCount = 3;

Console.WriteLine(queryPerfCounter(name, category, instance, maxSampleCount));
Console.WriteLine();

static float queryPerfCounter(string name, string category, string instance, int maxSampleCount)
{
    try
    {
        PerformanceCounter total_cpu = new(name, category, instance);
        int sampleCount = maxSampleCount;
        
        float result = 0;
        while (sampleCount >= 0)
        {
            if (sampleCount != maxSampleCount)
            {
                result += total_cpu.NextValue();
            }

            sampleCount--;
        }

        return (result / maxSampleCount);
    }
    catch (Exception)
    {

        return 0.0f;
    }
}